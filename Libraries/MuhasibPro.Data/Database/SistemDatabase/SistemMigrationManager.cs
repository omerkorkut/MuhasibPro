using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Concrete.Database.SistemDatabase;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.SistemDatabase
{
    public class SistemMigrationManager : ISistemMigrationManager
    {
        private readonly SistemDbContext _dbContext;
        private readonly ILogger<SistemMigrationManager> _logger;
        private readonly ISistemBackupManager _backupManager;
        private readonly IApplicationPaths _applicationPaths;
        private static readonly SemaphoreSlim _globalMigrationLock = new SemaphoreSlim(1, 1);
        private const int LOCK_TIMEOUT_SECONDS = 30; // Max 30 saniye

        private const string _databaseName = DatabaseConstants.SISTEM_DB_NAME;
        private static readonly string[] TablesToCheck = { nameof(Kullanici), nameof(AppDbVersion) };

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

        private async Task<bool> InternalInitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if(!_isSistemDatabaseFileExist)
                {
                    // ⭐ DATABASE YOK: YENİ OLUŞTUR
                    _logger.LogInformation("Database dosyası bulunamadı, yeni oluşturuluyor: {Database}", _databaseName);

                var createResult = await _dbContext.ExecuteCreatingDatabaseAsync(
                        databaseName: _databaseName,
                        logger: _logger,
                        commandTimeoutMinutes: 5,
                    ct: cancellationToken).ConfigureAwait(false);

                    if(createResult.IsCreatedSuccess)
                    {
                        // İlk kurulumda version kaydı oluştur
                        await DatabaseVersionFromMigrationsAsync(_dbContext, cancellationToken);
                        return true;
                    }

                    _logger.LogError("Database oluşturma başarısız: {Database}", _databaseName);
                    return false;
                } else
                {
                    // ⭐ DATABASE VAR: GÜNCELLE
                    _logger.LogInformation(
                        "Database dosyası mevcut, güncelleme kontrol ediliyor: {Database}",
                        _databaseName);

                    // Önce analiz et (sadece log için)
                    var analysis = await _dbContext.GetConnectionFullStateAsync(
                        databaseName: _databaseName,
                        isDatabaseExists: _isSistemDatabaseFileExist,
                        databaseValid: _isSistemDatabaseValid,
                        tablesToCheck: TablesToCheck,
                        logger: _logger,
                        ct: cancellationToken).ConfigureAwait(false);

                    if(!analysis.CanConnect)
                    {
                        _logger.LogError("Database'e bağlanılamıyor: {Database}", _databaseName);
                        return false;
                    }

                    // Migration uygula
                    // Local wrapper functions to ensure correct delegate signatures and proper ConfigureAwait usage
                    async Task<bool> RestoreWrapper()
                    {
                        return await _backupManager.RestoreFromLatestBackupAsync(cancellationToken).ConfigureAwait(false);
                    }

                    async Task<bool> BackupWrapper()
                    {
                        var res = await _backupManager.CreateBackupAsync(DatabaseBackupType.Automatic, cancellationToken).ConfigureAwait(false);
                        return res != null && res.IsBackupComleted;
                    }

                    var migrationResult = await _dbContext.ExecuteMigrationsWithBackupCheckAsync(
                        databaseName: _databaseName,
                        isDatabaseExists: _isSistemDatabaseFileExist,
                        databaseValid: _isSistemDatabaseValid,
                        tablesToCheck: TablesToCheck,
                        restoreAction: RestoreWrapper,
                        backupAction: BackupWrapper,
                        logger: _logger,
                        commandTimeoutMinutes: 5,
                        ct: cancellationToken).ConfigureAwait(false);

                    if(migrationResult.IsHealthy)
                    {
                        // ⭐ VERSİYONU GÜNCELLE (migration'dan SONRA)
                        await DatabaseVersionFromMigrationsAsync(_dbContext, cancellationToken).ConfigureAwait(false);
                    }

                    return migrationResult.IsHealthy;
                }
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Initialize database hatası: {_databaseName}", _databaseName);
                return false;
            }
        }

        public async Task<bool> InitializeSistemDatabaseAsync(CancellationToken cancellationToken)
        {
            // ⭐ GLOBAL LOCK - tek thread migration
            try
            {
                if (!await _globalMigrationLock.WaitAsync(TimeSpan.FromSeconds(LOCK_TIMEOUT_SECONDS), cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogWarning("Migration system is busy, skipping...");
                    return false;
                }

                try
                {
                    return await InternalInitializeAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _globalMigrationLock.Release();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "InitializeSistemDatabaseAsync iptal edildi");
                return false;
            }
        }

        public async Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(
            CancellationToken cancellationToken)
        {
            using var context = _dbContext;
            var result = await context.GetConnectionFullStateAsync(
                _databaseName,
                _isSistemDatabaseFileExist,
                _isSistemDatabaseValid,
                TablesToCheck,
                _logger,
                cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<List<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var state = await GetSistemDatabaseStateAsync(cancellationToken).ConfigureAwait(false);
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

        public async Task<string> GetCurrentDatabaseVersionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var versionRecord = await _dbContext.AppDbVersiyonlar
                    .Where(v => v.DatabaseName == _databaseName)
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if(versionRecord != null)
                    return versionRecord.CurrentDatabaseVersion;

                // Version kaydı yoksa state'ten al
                var state = await GetSistemDatabaseStateAsync(cancellationToken).ConfigureAwait(false);
                return state.CurrentVersion;
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu alınamadı: {Database}", _databaseName);
                return "1.0.0.0";
            }
        }

        private async Task DatabaseVersionFromMigrationsAsync(SistemDbContext context, CancellationToken cancellationToken)
        {
            try
            {                

                var appliedMigrations = await context.Database
                    .GetAppliedMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);

                var latestMigration = appliedMigrations.LastOrDefault();
                if (string.IsNullOrEmpty(latestMigration))
                    return;

                var newVersion = DatabaseUtilityHelper.ExtractVersionFromMigration(latestMigration);

                // Transaction başlat
                await using var transaction = await context.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);

                var versionRecord = await context.AppDbVersiyonlar
                    .Where(v => v.DatabaseName == _databaseName)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (versionRecord == null)
                {
                    // AYNI CONTEXT'i kullan
                    var firstVersion = new AppDbVersion
                    {
                        DatabaseName = _databaseName,
                        CurrentDatabaseLastUpdate = DateTime.UtcNow,
                        CurrentDatabaseVersion = "1.0.0.0",
                        PreviousDatabaseVersion = null
                    };

                    await context.AppDbVersiyonlar
                        .AddAsync(firstVersion, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation(
                        "İlk versiyon kaydı oluşturuldu: {Database} - {Version}",
                        _databaseName,
                        newVersion);
                }
                else
                {
                    versionRecord.PreviousDatabaseVersion = versionRecord.CurrentDatabaseVersion;
                    versionRecord.CurrentDatabaseVersion = newVersion;
                    versionRecord.CurrentDatabaseLastUpdate = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Database versiyonu güncellendi: {Database} ({Old} → {New})",
                    _databaseName,
                    versionRecord?.PreviousDatabaseVersion ?? "null",
                    newVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu güncellenemedi: {Database}", _databaseName);
            }
        }


    }
}