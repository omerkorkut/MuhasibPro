using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Concrete.Database.SistemDatabase;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResult;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.SistemDatabase
{
    public class SistemBackupManager : ISistemBackupManager
    {
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger<SistemBackupManager> _logger;
        private readonly IDatabaseBackupManager _backupManager;

        private const int BACKUP_DELAY_MS = 50;
        private const string BACKUP_FILE_PATTERN = "{0}_*.backup";
        private const string TEMP_BACKUP_PATTERN = "{0}_before_restore_{1}.temp";

        private readonly string _databaseName = DatabaseConstants.SISTEM_DB_NAME;

        public SistemBackupManager(
            IApplicationPaths applicationPaths,
            ILogger<SistemBackupManager> logger,
            IDatabaseBackupManager backupManager)
        {
            _applicationPaths = applicationPaths;
            _logger = logger;
            _backupManager = backupManager;
        }

        public async Task<DatabaseBackupResult> CreateBackupAsync(
            DatabaseBackupType backupType,
            CancellationToken cancellationToken)
        {
            var result = new DatabaseBackupResult
            {
                DatabaseName = _databaseName,
                BackupType = backupType,
                LastBackupDate = DateTime.UtcNow,
                IsBackupComleted = false,
            };
            try
            {
                var sourceFilePath = _applicationPaths.GetSistemDatabaseFilePath();
                result.BackupFilePath = sourceFilePath;
                //ÖN KONTROLLER
                if (!File.Exists(sourceFilePath))
                {
                    _logger?.LogWarning("Yedekleme için kaynak dosya bulunamadı: {DatabaseName}", _databaseName);
                    result.Message = "🗃️ Yedek alınacak kaynak veritabanı bulunamadı!";
                    return result;
                }

                var backupDir = _applicationPaths.GetBackupFolderPath();
                var backupFileName = DatabaseNamingHelper.GenerateBackupFileName(databaseName: _databaseName);
                var backupPath = Path.Combine(backupDir, backupFileName);
                try
                {
                    // 1. WAL checkpoint
                    await _backupManager.ExecuteWalCheckpointAsync(sourceFilePath, _databaseName, cancellationToken);
                    // 2. WAL dosyalarını temizle
                    _backupManager.CleanupSqliteWalFiles(sourceFilePath);
                } catch(Exception ex)
                {
                    _logger?.LogError(ex, "Backup oluşturulamadı: {DatabaseName}", _databaseName);
                    result.Message = "🔴 Veritabanı hazırlık aşamasında (WAL) hata oluştu.";
                    
                    result.IsBackupComleted = false;
                    return result;
                }
                
                result.BackupDirectory = backupDir;
                result.BackupFileName = backupFileName;
                result.BackupPath = backupPath;
                // 4. Backup al
                try
                {
                    await _backupManager.SafeFileCopyAsync(sourceFilePath, backupPath, cancellationToken);
                    result.Message = "➕ Yedekleme işlemi başlatıldı...";
                    // 5. DOĞRULAMA
                    if(_backupManager.IsValidBackupFile(
                        Path.GetDirectoryName(backupPath),
                        Path.GetFileName(backupFileName)))
                    {
                        result.IsBackupComleted = true;
                        result.Message = "✅ Yedekleme işlemi başarıyla tamamlandı.";
                    } else
                    {
                        if(File.Exists(backupPath))
                            File.Delete(backupPath);

                        result.Message = "❌ Yedek dosyası oluşturulurken disk hatası oluştu.";
                        return result;
                    }
                } catch(OperationCanceledException ex)
                {
                    // 6. Hata
                    _logger.LogError(ex, "Yedekleme iptal edildi!");                    
                    result.Message = "⏹️ Yedekleme iptal edildi!";
                }

                _logger?.LogInformation(
                "Veritabanı yedeklendi: {DatabaseName} -> {BackupPath}",
                _databaseName,
                result.BackupFileName);
                
                return result;
            } catch(Exception ex)
            {
                _logger?.LogError(ex, "Veritabanı yedeklenemedi: {DatabaseName}", _databaseName);
                return result;
            }
        }

        public async Task<bool> RestoreFromLatestBackupAsync(CancellationToken cancellationToken)
        {
            // 1. Tüm backup'ları listele
            var backups = await GetBackupsAsync();

            // 2. ✅ SADECE geçerli (IsValid = true) backup'ları düşün
            var validBackups = backups.Where(b => b.IsBackupComleted).ToList();

            if(!validBackups.Any())
                return false;

            // 3. En yeni GEÇERLİ backup'ı bul
            var latestBackup = validBackups
                .OrderByDescending(b => b.LastBackupDate)
                .FirstOrDefault();

            if(latestBackup == null)
                return false;

            // 4. O backup'ı geri yükle
            return await RestoreBackupAsync(latestBackup.BackupFileName, cancellationToken);
        }

        public Task<List<DatabaseBackupResult>> GetBackupsAsync()
        {
            try
            {
                var backupDir = _applicationPaths.GetBackupFolderPath();
                if(!Directory.Exists(backupDir))
                    return Task.FromResult(new List<DatabaseBackupResult>());

                var searchPattern = string.Format(BACKUP_FILE_PATTERN, _databaseName);

                var backupList = new List<DatabaseBackupResult>();

                foreach(var filePath in Directory.GetFiles(backupDir, searchPattern))
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);

                        // ✅ HEM IsValidBackupFile HEM IsDatabaseFileValid
                        var isValidBackup = _backupManager.IsValidBackupFile(backupDir, fileInfo.Name);
                        var isSqliteValid = _applicationPaths.IsDatabaseFileValid(filePath);

                        var backup = new DatabaseBackupResult
                        {
                            BackupFileName = fileInfo.Name,
                            BackupFilePath = fileInfo.FullName,
                            BackupFileSizeBytes = fileInfo.Length,
                            LastBackupDate = fileInfo.CreationTimeUtc,
                            BackupType = _backupManager.DetermineBackupType(fileInfo.Name),
                            DatabaseName = _databaseName,
                            IsBackupComleted = isValidBackup && isSqliteValid, // ✅ İkisi birden
                        };

                        backupList.Add(backup);
                    } catch
                    {
                        // Geçersiz dosya, atla
                        continue;
                    }
                }

                return Task.FromResult(backupList.OrderByDescending(b => b.LastBackupDate).ToList());
            } catch(Exception ex)
            {
                _logger?.LogDebug(ex, "Backup listesi alınamadı: {DatabaseName}", _databaseName);
                return Task.FromResult(new List<DatabaseBackupResult>());
            }
        }

        public DateTime? GetLastBackupDate()
        {
            try
            {
                var backupDir = _applicationPaths.GetBackupFolderPath();
                if(!Directory.Exists(backupDir))
                    return null;

                var searchPattern = string.Format(BACKUP_FILE_PATTERN, _databaseName);
                var lastBackup = Directory.GetFiles(backupDir, searchPattern)
                    .Select(filePath => new FileInfo(filePath))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .FirstOrDefault();

                return lastBackup?.CreationTimeUtc;
            } catch(Exception ex)
            {
                _logger?.LogDebug(ex, "Son backup tarihi alınamadı: {DatabaseName}", _databaseName);
                return null;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupFileName, CancellationToken cancellationToken)
        {
            try
            {
                var restoreResult = await RestoreBackupDetailsAsync(backupFileName, cancellationToken);
                return restoreResult.IsRestoreSuccess;
            } catch(Exception ex)
            {
                _logger?.LogDebug(ex, "Son geri yükleme işlemi başarısız: {DatabaseName}", _databaseName);
                return false;
            }
        }

        public async Task<DatabaseRestoreExecutionResult> RestoreBackupDetailsAsync(
            string backupFileName,
            CancellationToken cancellationToken)
        {
            var result = new DatabaseRestoreExecutionResult
            {
                DatabaseName = _databaseName,
                OperationTime = DateTime.UtcNow,
                IsRestoreSuccess = false,
                HasError = false
            };

            string tempRestorePath = null;
            string safetyBackupPath = null;

            try
            {
                var backupDir = _applicationPaths.GetBackupFolderPath();
                var backupPath = Path.Combine(backupDir, backupFileName);
                var targetPath = _applicationPaths.GetSistemDatabaseFilePath();

                // 1. VALIDATION
                if(!_backupManager.IsValidBackupFile(backupDir, backupFileName))
                {
                    result.HasError = true;
                    result.Message = "Seçilen yedek dosyası geçersiz veya bozuk.";
                    return result;
                }

                // 2. CLEAR CONNECTION POOLS
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                await Task.Delay(BACKUP_DELAY_MS * 2, cancellationToken);

                // 3. CREATE SAFETY BACKUP (ROLLBACK için)
                if(File.Exists(targetPath))
                {
                    var safetyBackupName = string.Format(
                        TEMP_BACKUP_PATTERN,
                        _databaseName,
                        DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                    safetyBackupPath = Path.Combine(backupDir, safetyBackupName);

                    await _backupManager.ExecuteWalCheckpointAsync(targetPath, _databaseName, cancellationToken);
                    await _backupManager.SafeFileCopyAsync(targetPath, safetyBackupPath, cancellationToken);

                    _logger?.LogInformation("Güvenlik yedeği alındı: {SafetyBackup}", safetyBackupName);
                }

                // 4. ATOMIC RESTORE PROCESS
                tempRestorePath = targetPath + ".restoring_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                // 4a. Backup'ı TEMP dosyasına kopyala
                await _backupManager.SafeFileCopyAsync(backupPath, tempRestorePath, cancellationToken);

                // 4b. TEMP dosyasını doğrula
                _backupManager.CleanupSqliteWalFiles(tempRestorePath);

                if(!_applicationPaths.IsDatabaseFileValid(tempRestorePath)) // Yeni method
                {
                    throw new InvalidOperationException("Restore edilen temp dosya geçersiz");
                }

                // 4c. Eski dosyayı sil ve TEMP'i ATOMIC MOVE et
                bool deleteOriginalSuccess = false;
                int retryCount = 0;

                while(!deleteOriginalSuccess && retryCount < 3)
                {
                    try
                    {
                        if(File.Exists(targetPath))
                            File.Delete(targetPath);

                        deleteOriginalSuccess = true;
                    } catch(IOException) when (retryCount < 2)
                    {
                        retryCount++;
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                        await Task.Delay(100 * retryCount, cancellationToken);
                    }
                }

                if(!deleteOriginalSuccess)
                    throw new IOException("Eski database dosyası silinemiyor, restore iptal");

                // ⭐⭐ CRITICAL: ATOMIC MOVE
                File.Move(tempRestorePath, targetPath);
                tempRestorePath = null; // Başarılı oldu, temizlemeyelim

                // 5. POST-RESTORE CLEANUP
                _backupManager.CleanupSqliteWalFiles(targetPath);

                // 6. FINAL VALIDATION
                if(!_applicationPaths.IsDatabaseFileValid(targetPath))
                {
                    // ROLLBACK safety backup'a
                    await RollbackToSafetyBackupAsync(targetPath, safetyBackupPath, cancellationToken);

                    result.HasError = true;
                    result.Message = "Restore sonrası doğrulama başarısız, safety backup'a geri dönüldü.";
                    return result;
                }

                // 7. SUCCESS - Cleanup safety backup
                if(safetyBackupPath != null && File.Exists(safetyBackupPath))
                {
                    try
                    {
                        File.Delete(safetyBackupPath);
                    } catch
                    { /* Log warning if needed */
                    }
                }

                // 8. RETURN SUCCESS
                result.IsRestoreSuccess = true;
                result.Message = $"{backupFileName} yedeği başarıyla geri yüklendi.";
                _logger?.LogInformation("Atomic restore başarılı: {DatabaseName}", _databaseName);
            } catch(Exception ex)
            {
                result.HasError = true;
                result.Message = $"Geri yükleme sırasında hata: {ex.Message}";
                _logger?.LogError(ex, "Atomic restore başarısız: {DatabaseName}", _databaseName);

                // Hata durumunda safety backup'a geri dön
                try
                {
                    var targetPath = _applicationPaths.GetSistemDatabaseFilePath();
                    await RollbackToSafetyBackupAsync(targetPath, safetyBackupPath, cancellationToken);
                } catch(Exception rollbackEx)
                {
                    _logger?.LogCritical(
                    rollbackEx,
                    "CRITICAL: Restore ROLLBACK de başarısız! Database bozuk olabilir: {DatabaseName}",
                    _databaseName);
                }
            } finally
            {
                // TEMP dosyalarını temizle
                try
                {
                    if(tempRestorePath != null && File.Exists(tempRestorePath))
                        File.Delete(tempRestorePath);
                } catch
                { /* ignore */
                }
            }

            return result;
        }

        private async Task<bool> RollbackToSafetyBackupAsync(
            string targetPath,
            string safetyBackupPath,
            CancellationToken ct)
        {
            if(safetyBackupPath == null || !File.Exists(safetyBackupPath))
                return false;

            try
            {
                _logger?.LogWarning("Restore başarısız, safety backup'a geri dönülüyor...");

                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                await Task.Delay(BACKUP_DELAY_MS, ct);

                if(File.Exists(targetPath))
                    File.Delete(targetPath);

                await _backupManager.SafeFileCopyAsync(safetyBackupPath, targetPath, ct);
                _backupManager.CleanupSqliteWalFiles(targetPath);

                _logger?.LogInformation("Safety backup'a başarıyla geri dönüldü.");

                // Safety backup'ı sil
                try
                {
                    File.Delete(safetyBackupPath);
                } catch
                {
                }

                return true;
            } catch(Exception ex)
            {
                _logger?.LogError(ex, "Safety backup'a geri dönülemedi!");
                return false;
            }
        }

        public async Task<int> CleanOldBackupsAsync(int keepLast = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var backupDir = _applicationPaths.GetBackupFolderPath();
                return await _backupManager.CleanOldBackupsAsync(backupDir, _databaseName, keepLast, cancellationToken);
            } catch(Exception ex)
            {
                _logger?.LogError(ex, "Eski backup'lar temizlenemedi: {DatabaseName}", _databaseName);
                return 0;
            }
        }
    }
}