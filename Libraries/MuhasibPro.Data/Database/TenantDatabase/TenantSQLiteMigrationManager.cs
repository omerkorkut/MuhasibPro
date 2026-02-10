using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.TenantDatabase
{
    public class TenantSQLiteMigrationManager : ITenantSQLiteMigrationManager
    {
        private readonly IAppDbContextFactory _dbContextFactory;
        private readonly ILogger<TenantSQLiteMigrationManager> _logger;
        private readonly ITenantSQLiteBackupManager _backupManager;
        private readonly IApplicationPaths _applicationPaths;
        private static readonly SemaphoreSlim _globalMigrationLock = new SemaphoreSlim(1, 1);
        private const int LOCK_TIMEOUT_SECONDS = 30; // Max 30 saniye
        private static readonly string[] TablesToCheck = { nameof(TenantDatabaseVersiyon), nameof(AppLog) };

        public TenantSQLiteMigrationManager(
            IAppDbContextFactory dbContextFactory,
            ILogger<TenantSQLiteMigrationManager> logger,
            ITenantSQLiteBackupManager backupManager,
            IApplicationPaths applicationPaths)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _backupManager = backupManager;
            _applicationPaths = applicationPaths;
        }

        private (bool tenatFileExist, bool tenantDbValid) CheckTenantDatabaseState(string databaseName)
        {
            var tenantFileExist = _applicationPaths.TenantDatabaseFileExists(databaseName);
            var tenantDbValid = _applicationPaths.IsTenantDatabaseValid(databaseName);
            return (tenantFileExist, tenantDbValid);
        }
        public async Task<DatabaseMigrationExecutionResult> InitializeTenantDatabaseAsync(string databaseName)
        {
            var result = new DatabaseMigrationExecutionResult
            {
                DatabaseName = databaseName,
                IsUpdateRequired = false,
                IsRolledBack = false,
                AppliedMigrationsCount = 0,
                PendingMigrations = new List<string>(),
                DatabaseValid = false,
                OperationTime = DateTime.UtcNow,
                HasError = false
            };

            try
            {
                if (!await _globalMigrationLock.WaitAsync(TimeSpan.FromSeconds(LOCK_TIMEOUT_SECONDS)).ConfigureAwait(false))
                {
                    result.HasError = true;
                    result.Message = "Veritabanı migrasyonu için kilit alınamadı. Başka bir işlem devam ediyor olabilir.";
                    return result;
                }
                try
                {
                    var analysis = await GetTenantDatabaseStateAsync(databaseName).ConfigureAwait(false);
                    if (!analysis.IsHealthy)
                    {
                        result.HasError = true;
                        result.Message = analysis.Message;
                        return result;
                    }
                    async Task<bool> RestoreWrapper()
                    {
                        return await _backupManager.RestoreFromLatestBackupAsync(
                            databaseName: databaseName)
                            .ConfigureAwait(false);
                    }
                    async Task<bool> BackupWrapper()
                    {
                        var res = await _backupManager.CreateBackupAsync(databaseName, DatabaseBackupType.Migration).ConfigureAwait(false);
                        return res != null && res.IsBackupComleted;
                    }
                    using var _dbContext = _dbContextFactory.CreateDbContext(databaseName);
                    var migrationResult = await _dbContext.ExecuteTenantMigrationsWithBackupCheckAsync(
                        databaseName: databaseName,
                        isDatabaseExists: analysis.IsDatabaseExists,
                        databaseValid: analysis.DatabaseValid,
                        tablesToCheck: TablesToCheck,
                        restoreAction: RestoreWrapper,
                        backupAction: BackupWrapper,
                        logger: _logger)
                        .ConfigureAwait(false);
                    if (migrationResult.IsHealthy)
                    {
                        // ⭐ VERSİYONU GÜNCELLE (migration'dan SONRA)
                        await TenantDbVersionFromMigrationsAsync(_dbContext, databaseName).ConfigureAwait(false);
                    }
                    result = migrationResult;
                    return migrationResult;
                }
                finally
                {
                    _globalMigrationLock.Release();
                }
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = "Tenant database başlatılırken beklenmeyen bir hata oluştu.";
                _logger.LogError(ex, "Initialize database hatası: {DatabaseName}", databaseName);
                return result;
            }
        }
        public async Task<DatabaseCreatingExecutionResult> CreateNewTenantDatabase(
            string databaseName)
        {
            var result = new DatabaseCreatingExecutionResult
            {
                DatabaseName = databaseName,
                IsCreatedSuccess = false,
                CanConnect = false,
                OperationTime = DateTime.UtcNow,
                HasError = false
            };
            try
            {
                var tenantdbState = CheckTenantDatabaseState(databaseName);
                if (tenantdbState.tenatFileExist)
                {
                    result.HasError = true;
                    result.Message = "Veritabanı zaten mevcut.";
                    return result;
                }

                using var dbContext = _dbContextFactory.CreateDbContext(databaseName);
                var createResult = await dbContext.ExecuteCreatingDatabaseAsync(
                    databaseName: databaseName,
                    logger: _logger)
                    .ConfigureAwait(false);
                if (!createResult.IsCreatedSuccess)
                {
                    result.HasError = createResult.HasError;
                    result.Message = createResult.Message;
                    return result;
                }
                result = createResult;
                await TenantDbVersionFromMigrationsAsync(dbContext, databaseName).ConfigureAwait(false);
                _logger.LogInformation("Yeni tenant database oluşturuldu: {Database}", databaseName);
                return result;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = "Tenant database oluşturulurken beklenmeyen bir hata oluştu.";
                _logger.LogError(ex, "Veritabanı oluşturma hatası: {DatabaseName}", databaseName);
                return result;
            }
        }

        public async Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(
            string databaseName)
        {
            var tenantdbState = CheckTenantDatabaseState(databaseName);

            // Extension metodun tüm işi bitene kadar context'i hayatta tut
            using var context = _dbContextFactory.CreateDbContext(databaseName);

            // Extension metod SONUÇ DÖNENE KADAR context kullanacak
            var result = await context.GetConnectionFullStateAsync(
                databaseName: databaseName,
                isDatabaseExists: tenantdbState.tenatFileExist,
                databaseValid: tenantdbState.tenantDbValid,
                tablesToCheck: TablesToCheck,
                logger: _logger)
                .ConfigureAwait(false);
            if (result.IsDatabaseExists)
            {
                result.DatabaseFileSizeBytes = _applicationPaths.GetTenantDatabaseSize(databaseName);
            }

            // Extension metod tamamlandı, şimdi context dispose edilebilir
            return result;
        }

        public async Task<List<string>> GetTenantPendingMigrationsAsync(
            string databaseName)
        {
            try
            {
                var state = await GetTenantDatabaseStateAsync(databaseName).ConfigureAwait(false);
                return state.PendingMigrations;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetPendingMigrationsAsync iptal edildi");
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen migration'lar alınamadı: {Database}", databaseName);
                return new List<string>();
            }
        }

        public async Task<string> GetTenantCurrentDatabaseVersionAsync(
            string databaseName)
        {
            try
            {
                using var _dbContext = _dbContextFactory.CreateDbContext(databaseName);
                var versionRecord = await _dbContext.TenantDatabaseVersiyonlar
                    .Where(v => v.DatabaseName == databaseName)
                    .AsNoTracking()
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (versionRecord != null)
                    return versionRecord.CurrentTenantDbVersion;

                // Version kaydı yoksa state'ten al
                var state = await GetTenantDatabaseStateAsync(databaseName).ConfigureAwait(false);
                return state.CurrentVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu alınamadı: {Database}", databaseName);
                return "1.0.0.0";
            }
        }

        private async Task TenantDbVersionFromMigrationsAsync(AppDbContext context, string databaseName)
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

                // Transaction başlat
                await using var transaction = await context.Database
                    .BeginTransactionAsync()
                    .ConfigureAwait(false);

                var versionRecord = await context.TenantDatabaseVersiyonlar
                    .Where(v => v.DatabaseName == databaseName)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (versionRecord == null)
                {
                    // AYNI CONTEXT'i kullan
                    var firstVersion = new TenantDatabaseVersiyon
                    {
                        DatabaseName = databaseName,
                        CurrentTenantDbLastUpdate = DateTime.UtcNow,
                        CurrentTenantDbVersion = newVersion,
                        PreviousTenantDbVersiyon = null
                    };

                    await context.TenantDatabaseVersiyonlar
                        .AddAsync(firstVersion)
                        .ConfigureAwait(false);

                    _logger.LogInformation(
                        "İlk versiyon kaydı oluşturuldu: {Database} - {Version}",
                        databaseName,
                        newVersion);
                }
                else
                {
                    versionRecord.PreviousTenantDbVersiyon = versionRecord.CurrentTenantDbVersion;
                    versionRecord.CurrentTenantDbVersion = newVersion;
                    versionRecord.CurrentTenantDbLastUpdate = DateTime.UtcNow;
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                _logger.LogInformation(
                    "Database versiyonu güncellendi: {Database} ({Old} → {New})",
                    databaseName,
                    versionRecord?.PreviousTenantDbVersiyon ?? "null",
                    newVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database versiyonu güncellenemedi: {Database}", databaseName);
            }
        }
    }
}
