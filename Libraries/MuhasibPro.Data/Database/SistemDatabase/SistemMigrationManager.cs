using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;

namespace MuhasibPro.Data.Database.SistemDatabase
{
    public class SistemMigrationManager : ISistemMigrationManager
    {
        private readonly SistemDbContext _dbContext;
        private readonly ILogger<SistemMigrationManager> _logger;
        private readonly ISistemBackupManager _backupManager;
        private readonly IApplicationPaths _applicationPaths;
        private static readonly SemaphoreSlim _globalMigrationLock = new SemaphoreSlim(1, 1);
        private const int LOCK_TIMEOUT_SECONDS = 30;

        private const string _databaseName = DatabaseConstants.SISTEM_DB_NAME;
        private static readonly string[] TablesToCheck =
        {
            nameof(Kullanici),
            nameof(AppDbVersion),
            nameof(Firma),
            nameof(MaliDonem),
            nameof(Hesap),
            nameof(SistemLog)
        };

        private bool _isSistemDatabaseValid => _applicationPaths.IsSistemDatabaseValid();
        private bool _isSistemDatabaseFileExist => _applicationPaths.SistemDatabaseFileExists();

        public SistemMigrationManager(
            ILogger<SistemMigrationManager> logger,
            ISistemBackupManager backupManager,
            IApplicationPaths applicationPaths,
            SistemDbContext dbContext)
        {
            _logger = logger;
            _backupManager = backupManager;
            _applicationPaths = applicationPaths;
            _dbContext = dbContext;
        }

        private async Task<(bool initializeState, string message)> InternalInitializeAsync()
        {
            try
            {
                if (!_isSistemDatabaseFileExist)
                {
                    // ⭐ DATABASE YOK: YENİ OLUŞTUR
                    _logger.LogInformation("Database dosyası bulunamadı, yeni oluşturuluyor: {Database}", _databaseName);

                    var createResult = await _dbContext.ExecuteCreatingDatabaseAsync(
                        databaseName: _databaseName,
                        logger: _logger,
                        commandTimeoutMinutes: 5)
                        .ConfigureAwait(false);

                    if (createResult.IsCreatedSuccess)
                    {
                        await DatabaseVersionFromMigrationsAsync(_dbContext);
                        return (initializeState: true, message: createResult.Message);
                    }

                    _logger.LogError("Database oluşturma başarısız: {Database}", _databaseName);
                    return (initializeState: false, message: createResult.Message);
                }
                else
                {
                    // ⭐ DATABASE VAR: ÖNCE ANALİZ ET
                    _logger.LogInformation(
                        "Database dosyası mevcut, analiz ediliyor: {Database}",
                        _databaseName);

                    var analysis = await _dbContext.GetConnectionFullStateAsync(
                        databaseName: _databaseName,
                        isDatabaseExists: _isSistemDatabaseFileExist,
                        databaseValid: _isSistemDatabaseValid,
                        tablesToCheck: TablesToCheck,
                        logger: _logger)
                        .ConfigureAwait(false);

                    if (!analysis.CanConnect)
                    {
                        _logger.LogError("Database'e bağlanılamıyor: {Database}", _databaseName);
                        return (initializeState: false, message: analysis.Message);
                    }

                    // ✅ ANALİZ SONUCUNU LOGLA
                    _logger.LogInformation(
                        "Database analizi: {Database}, Tablo: {TableCount}, Pending: {PendingCount}, BackupGerekli: {BackupRequired}",
                        _databaseName,
                        analysis.TableCount,
                        analysis.PendingMigrations?.Count ?? 0,
                        analysis.ShouldTakeBackupBeforeMigration);

                    // ⭐ MIGRATION UYGULA (ANALİZ PARAMETRESİ İLE)
                    async Task<bool> RestoreWrapper()
                    {
                        return await _backupManager.RestoreFromLatestBackupAsync()
                            .ConfigureAwait(false);
                    }

                    async Task<bool> BackupWrapper()
                    {
                        var res = await _backupManager.CreateBackupAsync(
                            DatabaseBackupType.Automatic)
                            .ConfigureAwait(false);
                        return res != null && res.IsBackupComleted;
                    }

                    // ✅ YENİ METOD: Analiz parametresi ile çağır
                    var migrationResult = await _dbContext.ExecuteMigrationsWithBackupCheckAsync(
                        analysis: analysis, // ✅ Önceden yapılmış analizi geç
                        restoreAction: RestoreWrapper,
                        backupAction: BackupWrapper,
                        logger: _logger,
                        commandTimeoutMinutes: 5) // ✅ HER ZAMAN BACKUP AL
                        .ConfigureAwait(false);

                    // ⭐ VERSİYONU GÜNCELLE
                    if (migrationResult.IsSuccess)
                    {
                        await DatabaseVersionFromMigrationsAsync(_dbContext).ConfigureAwait(false);
                    }

                    // ✅ DETAYLI LOG
                    _logger.LogInformation(
                        "Migration sonucu: {Database}, Başarı: {Success}, Backup: {Backup}, Migration: {MigrationApplied}",
                        _databaseName,
                        migrationResult.IsSuccess,
                        migrationResult.BackupTaken,
                        migrationResult.MigrationApplied);

                    return (initializeState: migrationResult.IsSuccess, migrationResult.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initialize database hatası: {Database}", _databaseName);
                return (initializeState: false, message: ex.Message);
            }
        }

        public async Task<(bool initializeState, string message)> InitializeSistemDatabaseAsync()
        {
            try
            {
                if (!await _globalMigrationLock.WaitAsync(TimeSpan.FromSeconds(LOCK_TIMEOUT_SECONDS))
                    .ConfigureAwait(false))
                {
                    _logger.LogWarning("Migration system is busy, skipping...");
                    return (initializeState: false, "❌ Sistem veritabanı oluşturulamadı, Sistem meşgul");
                }

                try
                {
                    return await InternalInitializeAsync();
                }
                finally
                {
                    _globalMigrationLock.Release();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "InitializeSistemDatabaseAsync iptal edildi");
                return (false, "❌ Sistem veritabanı başlatma işlemi iptal edildi");
            }
        }

        public async Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync()
        {
            var result = await _dbContext.GetConnectionFullStateAsync(
                _databaseName,
                _isSistemDatabaseFileExist,
                _isSistemDatabaseValid,
                TablesToCheck,
                _logger)
                .ConfigureAwait(false);

            if (result.IsDatabaseExists)
            {
                result.DatabaseFileSizeBytes = _applicationPaths.GetSistemDatabaseSize();
            }
            return result;
        }

        public async Task<List<string>> GetPendingMigrationsAsync()
        {
            try
            {
                var state = await GetSistemDatabaseStateAsync().ConfigureAwait(false);
                return state.PendingMigrations;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetPendingMigrationsAsync iptal edildi");
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen migration'lar alınamadı: {Database}", _databaseName);
                return new List<string>();
            }
        }

        public async Task<string> GetCurrentDatabaseVersionAsync()
        {
            try
            {
                var versionRecord = await _dbContext.AppDbVersiyonlar
                    .Where(v => v.DatabaseName == _databaseName)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (versionRecord != null)
                    return versionRecord.CurrentDatabaseVersion;

                var state = await GetSistemDatabaseStateAsync().ConfigureAwait(false);
                return state.CurrentVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu alınamadı: {Database}", _databaseName);
                return "1.0.0.0";
            }
        }

        public async Task<DatabaseHealtyDiagReport> GetSistemDatabaseFullDiagStateAsync(
            IProgress<AnalysisProgress> progressReporter = null,
            AnalysisOptions options = null)
        {
            var progress = progressReporter ?? new Progress<AnalysisProgress>();
            var optionAnalysis = options ??
                new AnalysisOptions
                {
                    CheckIntegrity = true,
                    CheckMigrations = true,
                    BatchSize = 5,
                    DelayBetweenBatches = TimeSpan.FromMilliseconds(100),
                };

            DatabaseHealtyDiagReport analysis;

            try
            {
                analysis = await _dbContext.GetDatabaseFullDiagStateAsync(
                    databaseName: _databaseName,
                    isDatabaseExists: _isSistemDatabaseFileExist,
                    databaseValid: _isSistemDatabaseValid,
                    tablesToCheck: TablesToCheck,
                    progressReporter: progress,
                    options: optionAnalysis,
                    logger: _logger);

                EnsureFileSizeInfoAsync(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database analizi başarısız");
                analysis = CreateErrorReport(ex);
            }

            if (analysis.HasError)
            {
                await CleanupOnErrorAsync();
            }

            return analysis;
        }

        private void EnsureFileSizeInfoAsync(DatabaseHealtyDiagReport analysis)
        {
            if (analysis.IsDatabaseExists && analysis.DatabaseFileSizeBytes == 0)
            {
                try
                {
                    var fileSize = _applicationPaths.GetSistemDatabaseSize();
                    if (fileSize > 0)
                    {
                        analysis.DatabaseFileSizeBytes = fileSize;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Dosya boyutu bilgisi güncellenemedi");
                }
            }
        }

        private DatabaseHealtyDiagReport CreateErrorReport(Exception ex)
        {
            return new DatabaseHealtyDiagReport
            {
                DatabaseName = _databaseName,
                HasError = true,
                Message = ex.Message,
                IsDatabaseExists = _isSistemDatabaseFileExist,
                CanConnect = false,
                DatabaseValid = false,
                OperationTime = DateTime.UtcNow
            };
        }

        private async Task CleanupOnErrorAsync()
        {
            try
            {
                await _dbContext.Database.CloseConnectionAsync();
                await Task.Delay(100);
            }
            catch
            {
                // Dispose etmiyoruz
            }
        }

        private async Task DatabaseVersionFromMigrationsAsync(
            SistemDbContext context)
        {
            try
            {
                var appliedMigrations = await context.Database
                    .GetAppliedMigrationsAsync()
                    .ConfigureAwait(false);

                var latestMigration = appliedMigrations.LastOrDefault();
                if (string.IsNullOrEmpty(latestMigration))
                    return;

                var newVersion = DatabaseUtilityExtensionsHelper.ExtractVersionFromMigration(latestMigration);

                await using var transaction = await context.Database
                    .BeginTransactionAsync()
                    .ConfigureAwait(false);

                var versionRecord = await context.AppDbVersiyonlar
                    .Where(v => v.DatabaseName == _databaseName)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (versionRecord == null)
                {
                    var firstVersion = new AppDbVersion
                    {
                        DatabaseName = _databaseName,
                        CurrentDatabaseLastUpdate = DateTime.UtcNow,
                        CurrentDatabaseVersion = "1.0.0.0",
                        PreviousDatabaseVersion = null
                    };

                    await context.AppDbVersiyonlar.AddAsync(firstVersion).ConfigureAwait(false);
                }
                else
                {
                    versionRecord.PreviousDatabaseVersion = versionRecord.CurrentDatabaseVersion;
                    versionRecord.CurrentDatabaseVersion = newVersion;
                    versionRecord.CurrentDatabaseLastUpdate = DateTime.UtcNow;
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                _logger.LogInformation(
                    "Database versiyonu güncellendi: {Database} → {New}",
                    _databaseName,
                    newVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu güncellenemedi: {Database}", _databaseName);
            }
        }
    }
}