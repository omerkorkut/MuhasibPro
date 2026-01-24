using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.TenantDatabase
{
    public class TenantSQLiteDatabaseManager : ITenantSQLiteDatabaseManager
    {
        private readonly IApplicationPaths _applicationPaths;
        private readonly ITenantSQLiteMigrationManager _migrationManager;
        private readonly ILogger<TenantSQLiteDatabaseManager> _logger;
        private static readonly SemaphoreSlim _globalDeletionLock = new SemaphoreSlim(1, 1);
        private const int LOCK_TIMEOUT_SECONDS = 30; // Max 30 saniye

        public TenantSQLiteDatabaseManager(IApplicationPaths applicationPaths, ITenantSQLiteMigrationManager migrationManager, ILogger<TenantSQLiteDatabaseManager> logger)
        {
            _applicationPaths = applicationPaths;
            _migrationManager = migrationManager;
            _logger = logger;
        }

        private (bool tenantFileExist, bool tenantDbValid) CheckTenantDatabaseState(string databaseName)
        {
            var tenantFileExist = _applicationPaths.TenantDatabaseFileExists(databaseName);
            var tenantDbValid = _applicationPaths.IsTenantDatabaseValid(databaseName);
            return (tenantFileExist, tenantDbValid);
        }

        public async Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(
            string databaseName)
        {
            var analysis = new DatabaseConnectionAnalysis();
            try
            {
                var databaseHealty = await _migrationManager.GetTenantDatabaseStateAsync(
                    databaseName)
                    .ConfigureAwait(false);
                if (!databaseHealty.IsHealthy)
                {
                    databaseHealty.HasError = true;
                    databaseHealty.Message = "[Hata] ❌ Veritabanı durum analizi yapılamadı.";
                }
                analysis = databaseHealty;
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı analiz hatası: {DatabaseName}", databaseName);
                analysis.HasError = true;
                analysis.Message = "[Hata] ❌ Veritabanı durum analizi yapılamadı.";
                return analysis;
            }
        }

        public async Task<DatabaseCreatingExecutionResult> CreateNewTenantDatabaseAsync(
            string databaseName)
        {
            bool isRollbackNeeded = false;
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
                var createResult = await _migrationManager.CreateNewTenantDatabase(databaseName);

                // Migration başarısızsa onun result'ını döndür
                if (!createResult.IsCreatedSuccess)
                {
                    return createResult;
                }

                // Race condition için bekle
                await Task.Delay(500);

                var tenantdbState = CheckTenantDatabaseState(databaseName);

                // ✅ DÜZELTİLDİ: Migration başarılı ama dosya yoksa HATA
                if (!tenantdbState.tenantFileExist)
                {
                    result.HasError = true;
                    result.Message = "🔴 Veritabanı dosyası oluşturulamadı.";
                    return result;
                }

                // Dosya var ama geçersiz mi?
                isRollbackNeeded = !tenantdbState.tenantDbValid;

                if (isRollbackNeeded)
                {
                    result.HasError = true;
                    var deletingResult = await DeleteTenantDatabase(databaseName);

                    if (deletingResult.IsDeletedSuccess)
                    {
                        result.Message = "❌ Oluşturulan veritabanı geçersiz. İşlem geri alındı";
                    }
                    else
                    {
                        result.Message = deletingResult.Message;
                    }
                    return result;
                }

                // ✅ HER ŞEY BAŞARILI - migration manager'ın result'ını döndür
                return createResult;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = "Tenant database oluşturulurken beklenmeyen bir hata oluştu.";
                _logger.LogError(ex, "Veritabanı oluşturma hatası: {DatabaseName}", databaseName);
                return result;
            }
        }

        public async Task<DatabaseMigrationExecutionResult> InitializeTenantDatabaseAsync(
            string databaseName)
        {
            var result = new DatabaseMigrationExecutionResult();
            var validateState = await ValidateTenantDatabaseAsync(databaseName);
            if (!validateState.isValid)
            {
                result.HasError = true;
                result.Message = validateState.Message;
                return result;
            }
            try
            {
                var initializeDatabase = await _migrationManager.InitializeTenantDatabaseAsync(
                    databaseName)
                    .ConfigureAwait(false);
                if (initializeDatabase.HasError)
                {
                    result.HasError = initializeDatabase.HasError;
                    result.Message = initializeDatabase.Message;
                }

                return initializeDatabase;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Veritabanı başlatılamadı:, {databaseName}", databaseName);
                result.HasError = true;
                result.Message = $"[Hata] ❌ Veritabanı başlatılamadı:{ex.Message} ";
                return result;
            }
        }

        public async Task<DatabaseDeletingExecutionResult> DeleteTenantDatabase(
            string databaseName)
        {
            var deletingResult = new DatabaseDeletingExecutionResult
            {
                IsDeletedSuccess = false,
                HasError = true,
                OperationTime = DateTime.UtcNow,
            };

            var result = _applicationPaths.TenantDatabaseFileExists(databaseName);
            if (!result)
            {
                deletingResult.HasError = true;
                deletingResult.Message = "🔴 Silinecek veritabanı bulunamadı";
                return deletingResult;
            }
            try
            {
                if (!await _globalDeletionLock.WaitAsync(TimeSpan.FromSeconds(LOCK_TIMEOUT_SECONDS)))
                {
                    deletingResult.HasError = true;
                    deletingResult.Message = "🔴 Veritabanı silme işlemi zaman aşımına uğradı. Lütfen tekrar deneyin.";
                    return deletingResult;
                }
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        SqliteConnection.ClearAllPools();

                        await Task.Delay(100 * attempt); // Bağlantıların kapanması için bekleme süresi
                        var dbPath = _applicationPaths.GetTenantDatabaseFilePath(databaseName);
                        if (File.Exists(dbPath))
                        {
                            File.Delete(dbPath);
                            if (File.Exists(dbPath))
                            {
                                deletingResult.HasError = true;
                                deletingResult.Message = "�� Veritabanı silme işlemi başarısız oldu.";
                                return deletingResult;
                            }
                        }
                        _applicationPaths.CleanupSqliteWalFiles(databaseName);
                        deletingResult.IsDeletedSuccess = true;
                        deletingResult.Message = "✅ Veritabanı başarıyla silindi.";
                        return deletingResult;
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Veritabanı silme hatası (Deneme {Attempt}): {DatabaseName}",
                            attempt,
                            databaseName);
                        if (attempt == 3)
                        {
                            deletingResult.HasError = true;
                            deletingResult.Message = $"[Hata] ❌ Veritabanı kullanımda. Silinemedi: {ex.Message}";
                            return deletingResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Veritabanı silme hatası: {DatabaseName}", databaseName);
                        deletingResult.HasError = true;
                        deletingResult.Message = $"[Hata] ❌ Veritabanı silinemedi: {ex.Message}";
                        return deletingResult;
                    }
                }
            }
            finally
            {
                _globalDeletionLock.Release();
            }
            return deletingResult;
        }

        public async Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName)
        {
            try
            {
                var quickTest = CheckTenantDatabaseState(databaseName);
                if (!quickTest.tenantFileExist)
                {
                    return (isValid: false, "Veritabanı dosyası bulunamadı");
                }
                if (!quickTest.tenantDbValid)
                {
                    return (isValid: false, "Veritabanı dosya boyutu geçersiz.");
                }
                var result = await GetTenantDatabaseStateAsync(databaseName);
                return result?.ToLegacyResult() ?? (false, "Veritabanı durumu alınamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validate failed: {DatabaseName}", databaseName);
                return (false, $"Doğrulama başarısız: {ex.Message}");
            }
        }
    }
}
