using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Concrete.Database.SistemDatabase;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Entities.SistemEntity;
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
            SistemDbContext dbContext,
            ILogger<SistemMigrationManager> logger,
            ISistemBackupManager backupManager,
            IApplicationPaths applicationPaths)
        {
            _dbContext = dbContext;
            _logger = logger;
            _backupManager = backupManager;
            _applicationPaths = applicationPaths;
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
                        ct: cancellationToken);

                    if(createResult.IsCreatedSuccess)
                    {
                        // İlk kurulumda version kaydı oluştur
                        await CreateInitialVersionRecordAsync("1.0.0.0", cancellationToken);
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
                        ct: cancellationToken);

                    if(!analysis.CanConnect)
                    {
                        _logger.LogError("Database'e bağlanılamıyor: {Database}", _databaseName);
                        return false;
                    }

                    // Migration uygula
                    var migrationResult = await _dbContext.ExecuteMigrationsWithBackupCheckAsync(
                        databaseName: _databaseName,
                        isDatabaseExists: _isSistemDatabaseFileExist,
                        databaseValid: _isSistemDatabaseValid,
                        tablesToCheck: TablesToCheck,
                        restoreAction: async () => await _backupManager.RestoreFromLatestBackupAsync(cancellationToken),
                        backupAction: async () => await _backupManager.CreateBackupAsync(cancellationToken),
                        logger: _logger,
                        commandTimeoutMinutes: 5,
                        ct: cancellationToken);

                    if(migrationResult.IsHealthy)
                    {
                        // ⭐ VERSİYONU GÜNCELLE (migration'dan SONRA)
                        await UpdateDatabaseVersionFromMigrationsAsync(cancellationToken);
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
            if(!await _globalMigrationLock.WaitAsync(TimeSpan.FromSeconds(LOCK_TIMEOUT_SECONDS), cancellationToken))
            {
                _logger.LogWarning("Migration system is busy, skipping...");
                return false;
            }

            try
            {
                return await InternalInitializeAsync(cancellationToken);
            } finally
            {
                _globalMigrationLock.Release();
            }
        }

        public async Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(
            CancellationToken cancellationToken)
        {
            return await _dbContext.GetConnectionFullStateAsync(
                _databaseName,
                _isSistemDatabaseFileExist,
                _isSistemDatabaseValid,
                TablesToCheck,
                _logger,
                cancellationToken);
        }

        public async Task<List<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var state = await GetSistemDatabaseStateAsync(cancellationToken);
                return state.PendingMigrations;
            } catch(Exception ex)
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
                    .FirstOrDefaultAsync(cancellationToken);

                if(versionRecord != null)
                    return versionRecord.CurrentDatabaseVersion;

                // Version kaydı yoksa state'ten al
                var state = await GetSistemDatabaseStateAsync(cancellationToken);
                return state.CurrentVersion;
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu alınamadı: {Database}", _databaseName);
                return "1.0.0.0";
            }
        }

        private async Task CreateInitialVersionRecordAsync(string version, CancellationToken cancellationToken)
        {
            try
            {
                var initialVersion = new AppDbVersion
                {
                    DatabaseName = _databaseName,
                    CurrentDatabaseVersion = version,
                    PreviousDatabaseVersion = null,
                    CurrentDatabaseLastUpdate = DateTime.UtcNow,
                };

                _dbContext.AppDbVersiyonlar.Add(initialVersion);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("İlk versiyon kaydı oluşturuldu: {Database} - {Version}", _databaseName, version);
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Versiyon kaydı oluşturulamadı: {Database}", _databaseName);
                // Kritik değil, devam et
            }
        }

        private async Task UpdateDatabaseVersionFromMigrationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Migration'dan SONRA applied migration'ları al
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);

                var latestMigration = appliedMigrations.LastOrDefault();
                if(string.IsNullOrEmpty(latestMigration))
                    return;

                // Migration isminden versiyon üret
                var newVersion = ExtractVersionFromMigration(latestMigration);

                // Versiyonu güncelle
                var versionRecord = await _dbContext.AppDbVersiyonlar
                    .Where(v => v.DatabaseName == _databaseName)
                    .FirstOrDefaultAsync(cancellationToken);

                if(versionRecord == null)
                {
                    await CreateInitialVersionRecordAsync(newVersion, cancellationToken);
                } else
                {
                    versionRecord.PreviousDatabaseVersion = versionRecord.CurrentDatabaseVersion;
                    versionRecord.CurrentDatabaseVersion = newVersion;
                    versionRecord.CurrentDatabaseLastUpdate = DateTime.UtcNow;


                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Database versiyonu güncellendi: {Database} ({Old} → {New})",
                        _databaseName,
                        versionRecord.PreviousDatabaseVersion,
                        newVersion);
                }
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu güncellenemedi: {Database}", _databaseName);
                // Kritik değil, migration başarılı sayılabilir
            }
        }

        private string ExtractVersionFromMigration(string migrationName)
        {
            // Örnek: "20240115143000_InitialCreate" → "2024.01.15.1430"
            if(migrationName.Length >= 14 && migrationName.Take(14).All(char.IsDigit))
            {
                var ts = migrationName.AsSpan(0, 14);
                return $"{ts.Slice(0, 4)}.{ts.Slice(4, 2)}.{ts.Slice(6, 2)}.{ts.Slice(8, 4)}";
            }

            // Migration sayısına göre
            return $"1.0.0.{migrationName.GetHashCode() % 10000}";
        }
    }
}