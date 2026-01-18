using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.TenantDatabase
{
    public class TenantSQLiteBackupManager : ITenantSQLieBackupManager
    {
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger<TenantSQLiteBackupManager> _logger;
        private readonly IDatabaseBackupManager _backupManager;
        private const int BACKUP_DELAY_MS = 50;
        private const string BACKUP_FILE_PATTERN = "{0}_*.backup";
        private const string TEMP_BACKUP_PATTERN = "{0}_before_restore_{1}.temp";

        public TenantSQLiteBackupManager(
            IApplicationPaths applicationPaths,
            ILogger<TenantSQLiteBackupManager> logger,
            IDatabaseBackupManager backupManager)
        {
            _applicationPaths = applicationPaths;
            _logger = logger;
            _backupManager = backupManager;
        }

        private string GetTenantFilePath(string databaseName) => _applicationPaths.GetTenantDatabaseFilePath(
            databaseName);

        private string GetTenantBackupFolderPath => _applicationPaths.GetTenantBackupFolderPath();

        public async Task<int> CleanOldBackupsAsync(
            string databaseName,
            int keepLast = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var backupDir = GetTenantBackupFolderPath;
                if (backupDir == null)
                    return 0;
                return await _backupManager.CleanOldBackupsAsync(backupDir, databaseName, keepLast, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Eski backup'lar temizlenemedi: {DatabaseName}", databaseName);
                return 0;
            }
        }

        public async Task<DatabaseBackupResult> CreateBackupAsync(
            string databaseName,
            DatabaseBackupType databaseBackup,
            CancellationToken cancellationToken)
        {
            var result = new DatabaseBackupResult
            {
                DatabaseName = databaseName,
                BackupType = databaseBackup,
                LastBackupDate = DateTime.UtcNow,
                IsBackupComleted = false,
            };
            try
            {
                var sourcePath = GetTenantFilePath(databaseName);
                result.BackupFilePath = sourcePath;
                if (!File.Exists(sourcePath))
                {
                    _logger?.LogWarning("Yedekleme için kaynak dosya bulunamadı: {DatabaseName}", databaseName);
                    result.Message = "🗃️ Yedek alınacak kaynak veritabanı bulunamadı!";
                    return result;
                }
                var backupDir = GetTenantBackupFolderPath;
                var backupFileName = DatabaseUtilityExtensionsHelper.GenerateBackupFileName(databaseName);
                var backupPath = Path.Combine(backupDir, backupFileName);
                try
                {
                    await _backupManager.ExecuteWalCheckpointAsync(sourcePath, databaseName, cancellationToken);
                    _backupManager.CleanupSqliteWalFiles(sourcePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Backup oluşturulamadı: {DatabaseName}", databaseName);
                    result.Message = "🔴 Veritabanı hazırlık aşamasında (WAL) hata oluştu.";

                    result.IsBackupComleted = false;
                    return result;
                }
                result.BackupDirectory = backupDir;
                result.BackupFileName = backupFileName;
                result.BackupPath = backupPath;
                try
                {
                    await _backupManager.SafeFileCopyAsync(sourcePath, backupPath, cancellationToken);
                    result.Message = "➕ Yedekleme işlemi başlatıldı...";
                    // 5. DOĞRULAMA
                    if (_backupManager.IsValidBackupFile(
                        Path.GetDirectoryName(backupPath),
                        Path.GetFileName(backupFileName)))
                    {
                        result.IsBackupComleted = true;
                        result.Message = "✅ Yedekleme işlemi başarıyla tamamlandı.";
                    }
                    else
                    {
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);

                        result.Message = "❌ Yedek dosyası oluşturulurken disk hatası oluştu.";
                        return result;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    // 6. Hata
                    _logger.LogError(ex, "Yedekleme iptal edildi!");
                    result.Message = "⏹️ Yedekleme iptal edildi!";
                }
                _logger?.LogInformation(
                "Veritabanı yedeklendi: {DatabaseName} -> {BackupPath}",
                databaseName,
                result.BackupFileName);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Veritabanı yedeklenemedi: {DatabaseName}", databaseName);
                return result;
            }
        }

        public Task<List<DatabaseBackupResult>> GetBackupsAsync(string databaseName)
        {
            try
            {
                var backupDir = GetTenantBackupFolderPath;
                if (!Directory.Exists(backupDir))
                    return Task.FromResult(new List<DatabaseBackupResult>());
                var searchPattern = string.Format(BACKUP_FILE_PATTERN, GetTenantFilePath(databaseName));
                var backupList = new List<DatabaseBackupResult>();
                foreach (var filePath in Directory.GetFiles(backupDir, searchPattern))
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var isValidBackup = _backupManager.IsValidBackupFile(backupDir, fileInfo.Name);
                        var isSqliteValid = _applicationPaths.IsSqliteDatabaseFileValid(filePath);
                        var backup = new DatabaseBackupResult
                        {
                            BackupPath = filePath,
                            BackupDirectory = backupDir,
                            BackupFileName = fileInfo.Name,
                            BackupFilePath = fileInfo.FullName,
                            BackupFileSizeBytes = fileInfo.Length,
                            LastBackupDate = fileInfo.LastAccessTimeUtc,
                            BackupType = _backupManager.DetermineBackupType(fileInfo.Name),
                            DatabaseName = databaseName,
                            IsBackupComleted = isValidBackup && isSqliteValid, // ✅ İkisi birden
                        };
                        backupList.Add(backup);
                    }
                    catch
                    {
                        continue;
                    }
                }
                return Task.FromResult(backupList.OrderByDescending(b => b.LastBackupDate).ToList());
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Backup listesi alınamadı: {DatabaseName}", databaseName);
                return Task.FromResult(new List<DatabaseBackupResult>());
            }
        }

        public DateTime? GetLastBackupDate(string databaseName)
        {
            try
            {
                var backupDir = GetTenantBackupFolderPath;
                if (!Directory.Exists(backupDir))
                    return null;

                var searchPattern = string.Format(BACKUP_FILE_PATTERN, databaseName);
                var lastBackup = Directory.GetFiles(backupDir, searchPattern)
                    .Select(filePath => new FileInfo(filePath))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .FirstOrDefault();

                return lastBackup?.CreationTimeUtc;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Son backup tarihi alınamadı: {DatabaseName}", databaseName);
                return null;
            }
        }

        public async Task<DatabaseRestoreExecutionResult> RestoreBackupAsync(
            string databaseName,
            string backupFileName,
            CancellationToken cancellationToken)
        {
            var restore = new DatabaseRestoreExecutionResult();
            try
            {
                var restoreResult = await RestoreBackupDetailsAsync(databaseName, backupFileName, cancellationToken);
                if (restoreResult.IsRestoreSuccess)
                    return restoreResult;
                return restoreResult;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Son geri yükleme işlemi başarısız: {DatabaseName}", backupFileName);
                return restore;
            }
        }

        public async Task<DatabaseRestoreExecutionResult> RestoreBackupDetailsAsync(
            string databaseName,
            string backupFileName,
            CancellationToken cancellationToken)
        {
            var result = new DatabaseRestoreExecutionResult
            {
                DatabaseName = databaseName,
                OperationTime = DateTime.UtcNow,
                IsRestoreSuccess = false,
                HasError = false
            };

            string tempRestorePath = null;
            string safetyBackupPath = null;

            try
            {
                var backupDir = GetTenantBackupFolderPath;
                var backupPath = Path.Combine(backupDir, backupFileName);
                var targetPath = GetTenantFilePath(databaseName);

                // 1. VALIDATION
                if (!_backupManager.IsValidBackupFile(backupDir, backupFileName))
                {
                    result.HasError = true;
                    result.Message = "Seçilen yedek dosyası geçersiz veya bozuk.";
                    return result;
                }

                // 2. CLEAR CONNECTION POOLS
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                await Task.Delay(BACKUP_DELAY_MS * 2, cancellationToken);

                // 3. CREATE SAFETY BACKUP (ROLLBACK için)
                if (File.Exists(targetPath))
                {
                    var safetyBackupName = string.Format(
                        TEMP_BACKUP_PATTERN,
                        databaseName,
                        DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                    safetyBackupPath = Path.Combine(backupDir, safetyBackupName);

                    await _backupManager.ExecuteWalCheckpointAsync(targetPath, databaseName, cancellationToken);
                    await _backupManager.SafeFileCopyAsync(targetPath, safetyBackupPath, cancellationToken);

                    _logger?.LogInformation("Güvenlik yedeği alındı: {SafetyBackup}", safetyBackupName);
                }

                // 4. ATOMIC RESTORE PROCESS
                tempRestorePath = targetPath + ".restoring_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                // 4a. Backup'ı TEMP dosyasına kopyala
                await _backupManager.SafeFileCopyAsync(backupPath, tempRestorePath, cancellationToken);

                // 4b. TEMP dosyasını doğrula
                _backupManager.CleanupSqliteWalFiles(tempRestorePath);

                if (!_applicationPaths.IsSqliteDatabaseFileValid(tempRestorePath)) // Yeni method
                {
                    throw new InvalidOperationException("Restore edilen temp dosya geçersiz");
                }

                // 4c. Eski dosyayı sil ve TEMP'i ATOMIC MOVE et
                bool deleteOriginalSuccess = false;
                int retryCount = 0;

                while (!deleteOriginalSuccess && retryCount < 3)
                {
                    try
                    {
                        if (File.Exists(targetPath))
                            File.Delete(targetPath);

                        deleteOriginalSuccess = true;
                    }
                    catch (IOException) when (retryCount < 2)
                    {
                        retryCount++;
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                        await Task.Delay(100 * retryCount, cancellationToken);
                    }
                }

                if (!deleteOriginalSuccess)
                    throw new IOException("Eski database dosyası silinemiyor, restore iptal");

                // ⭐⭐ CRITICAL: ATOMIC MOVE
                File.Move(tempRestorePath, targetPath);
                tempRestorePath = null; // Başarılı oldu, temizlemeyelim

                // 5. POST-RESTORE CLEANUP
                _backupManager.CleanupSqliteWalFiles(targetPath);

                // 6. FINAL VALIDATION
                if (!_applicationPaths.IsSqliteDatabaseFileValid(targetPath))
                {
                    // ROLLBACK safety backup'a
                    await RollbackToSafetyBackupAsync(targetPath, safetyBackupPath, cancellationToken);

                    result.HasError = true;
                    result.Message = "Restore sonrası doğrulama başarısız, safety backup'a geri dönüldü.";
                    return result;
                }

                // 7. SUCCESS - Cleanup safety backup
                if (safetyBackupPath != null && File.Exists(safetyBackupPath))
                {
                    try
                    {
                        File.Delete(safetyBackupPath);
                    }
                    catch
                    { /* Log warning if needed */
                    }
                }

                // 8. RETURN SUCCESS
                result.IsRestoreSuccess = true;
                result.Message = $"{backupFileName} yedeği başarıyla geri yüklendi.";
                _logger?.LogInformation("Atomic restore başarılı: {DatabaseName}", databaseName);
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"Geri yükleme sırasında hata: {ex.Message}";
                _logger?.LogError(ex, "Atomic restore başarısız: {DatabaseName}", databaseName);

                // Hata durumunda safety backup'a geri dön
                try
                {
                    var targetPath = _applicationPaths.GetSistemDatabaseFilePath();
                    await RollbackToSafetyBackupAsync(targetPath, safetyBackupPath, cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger?.LogCritical(
                    rollbackEx,
                    "CRITICAL: Restore ROLLBACK de başarısız! Database bozuk olabilir: {DatabaseName}",
                    databaseName);
                }
            }
            finally
            {
                // TEMP dosyalarını temizle
                try
                {
                    if (tempRestorePath != null && File.Exists(tempRestorePath))
                        File.Delete(tempRestorePath);
                }
                catch
                { /* ignore */
                }
            }

            return result;
        }

        public async Task<bool> RestoreFromLatestBackupAsync(string databaseName, CancellationToken cancellationToken)
        {
            // 1. Tüm backup'ları listele
            var backups = await GetBackupsAsync(databaseName);

            // 2. ✅ SADECE geçerli (IsValid = true) backup'ları düşün
            var validBackups = backups.Where(b => b.IsBackupComleted).ToList();

            if (!validBackups.Any())
                return false;

            // 3. En yeni GEÇERLİ backup'ı bul
            var latestBackup = validBackups
                .OrderByDescending(b => b.LastBackupDate)
                .FirstOrDefault();

            if (latestBackup == null)
                return false;

            // 4. O backup'ı geri yükle
            var restoreBackup = await RestoreBackupDetailsAsync(databaseName, latestBackup.BackupFileName, cancellationToken);
            if (restoreBackup.IsRestoreSuccess)
                return true;
            return false;
        }
        private async Task<bool> RollbackToSafetyBackupAsync(
        string targetPath,
        string safetyBackupPath,
        CancellationToken ct)
        {
            if (safetyBackupPath == null || !File.Exists(safetyBackupPath))
                return false;

            try
            {
                _logger?.LogWarning("Restore başarısız, safety backup'a geri dönülüyor...");

                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                await Task.Delay(BACKUP_DELAY_MS, ct);

                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                await _backupManager.SafeFileCopyAsync(safetyBackupPath, targetPath, ct);
                _backupManager.CleanupSqliteWalFiles(targetPath);

                _logger?.LogInformation("Safety backup'a başarıyla geri dönüldü.");

                // Safety backup'ı sil
                try
                {
                    File.Delete(safetyBackupPath);
                }
                catch
                {
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Safety backup'a geri dönülemedi!");
                return false;
            }
        }
    }
}
