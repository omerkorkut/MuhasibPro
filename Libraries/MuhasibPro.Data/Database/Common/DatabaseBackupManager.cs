using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using System.Buffers;

namespace MuhasibPro.Data.Database.Common
{
    public class DatabaseBackupManager : IDatabaseBackupManager
    {
        private readonly ILogger<DatabaseBackupManager> _logger;
        private const int BUFFER_SIZE = 81920; // 80KB
        private const int LOCK_RETRY_DELAY = 50;

        public DatabaseBackupManager(ILogger<DatabaseBackupManager> logger) { _logger = logger; }

        public void CleanupSqliteWalFiles(string dbFilePath)
        {
            if(string.IsNullOrEmpty(dbFilePath))
                return;

            try
            {
                var files = new[] { dbFilePath + "-wal", dbFilePath + "-shm" };
                foreach(var file in files)
                {
                    try
                    {
                        if(File.Exists(file))
                            File.Delete(file);
                    } catch(IOException)
                    { /* Dosya kullanımda */
                    }
                }
            } catch(Exception ex)
            {
                _logger?.LogDebug(ex, "WAL cleanup failed: {File}", Path.GetFileName(dbFilePath));
            }
        }

        public async Task ExecuteWalCheckpointAsync(
            string dbFilePath,
            string databaseName,
            CancellationToken cancellationToken)
        {
            if(!File.Exists(dbFilePath))
                return;

            try
            {
                await using var connection = new SqliteConnection($"Data Source={dbFilePath};Pooling=False");
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                await command.ExecuteScalarAsync(cancellationToken);

                _logger?.LogDebug("WAL checkpoint tamamlandı: {Database}", databaseName);
            } catch(Exception ex)
            {
                _logger?.LogDebug(ex, "WAL checkpoint başarısız: {Database}", databaseName);
            }
        }

        public async Task<DatabaseRestoreExecutionResult> ExecuteRestoreAsync(
            string databaseName,
            string backupSourcePath,
            string targetDbPath,
            CancellationToken ct)
        {
            var result = new DatabaseRestoreExecutionResult
            {
                IsRestoreSuccess = false,
                DatabaseName = databaseName,
                OperationTime = DateTime.UtcNow
            };

            string originalDbBackup = targetDbPath + ".before_restore.bak";

            try
            {
                // 1. ÖN KONTROLLER
                if (!File.Exists(backupSourcePath))
                {
                    result.Message = "Yedek dosyası bulunamadı.";
                    return result;
                }

                // 2. TÜM BAĞLANTILARI ZORLA KAPAT
                SqliteConnection.ClearAllPools();
                await Task.Delay(200, ct);

                // ⭐⭐ KRİTİK: RESTORE ÖNCESİ ÇİFT LOCK
                using (await AcquireFileLockAsync(backupSourcePath, TimeSpan.FromSeconds(10), ct))
                using (await AcquireFileLockAsync(targetDbPath, TimeSpan.FromSeconds(10), ct))
                {
                    // 3. MEVCUT DOSYAYI KORUMAYA AL (Artık GÜVENLİ)
                    if (File.Exists(targetDbPath))
                    {
                        CleanupSqliteWalFiles(targetDbPath);
                        File.Move(targetDbPath, originalDbBackup, overwrite: true);
                    }

                    // 4. RESTORE İŞLEMİ (locksuz kopyala - zaten lock'lar elimizde)
                    try
                    {
                        await SafeFileCopyAsync(backupSourcePath, targetDbPath, ct);

                        // 5. DOĞRULAMA
                        if (IsValidBackupFile(Path.GetDirectoryName(targetDbPath), Path.GetFileName(targetDbPath)))
                        {
                            result.IsRestoreSuccess = true;
                            result.Message = "Geri yükleme başarıyla tamamlandı.";
                            if (File.Exists(originalDbBackup))
                                File.Delete(originalDbBackup);
                        }
                        else
                        {
                            throw new Exception("Geri yüklenen dosya doğrulanamadı.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 6. ROLLBACK
                        _logger.LogError(ex, "Restore sırasında hata! Orijinal dosya geri yükleniyor.");
                        result.HasError = true;
                        result.Message = "Restore sırasında hata! Orijinal dosya geri yüklendi";
                        if (File.Exists(originalDbBackup))
                        {
                            File.Move(originalDbBackup, targetDbPath, overwrite: true);
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = $"Geri yükleme başarısız: {ex.Message}";
                _logger.LogError(ex, result.Message);
            }

            return result;
        }

        public async Task SafeFileCopyWithLockAsync(string source, string dest, CancellationToken cancellationToken)
        {
            using (await AcquireFileLockAsync(source, TimeSpan.FromSeconds(10), cancellationToken))
            {
                await SafeFileCopyAsync(source, dest, cancellationToken);
            }
        }
        public async Task SafeFileCopyAsync(string source, string dest, CancellationToken cancellationToken)
        {
            // === BAŞINA BU 5 SATIRI EKLEYİN ===
            if(string.Equals(source, dest, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Kaynak ve hedef aynı dosya olamaz", nameof(dest));

            var sourceDir = Path.GetDirectoryName(source);
            var destDir = Path.GetDirectoryName(dest);

            if(!string.Equals(sourceDir, destDir, StringComparison.OrdinalIgnoreCase))
            {
                var destDrive = Path.GetPathRoot(dest);
                if(DriveInfo.GetDrives()
                    .Any(
                        d => string.Equals(
                                d.Name.TrimEnd('\\'),
                                destDrive.TrimEnd('\\'),
                                StringComparison.OrdinalIgnoreCase) &&
                            d.AvailableFreeSpace < 1024 * 1024 * 10)) // 10MB
                {
                    throw new IOException("Hedef diskte yeterli alan yok");
                }
            }

            if(!File.Exists(source))
                throw new FileNotFoundException("Kaynak dosya bulunamadı", source);
            
                var tempDest = dest + ".tmp";

                try
                {
                    await TryReleaseFileLocksAsync(source, cancellationToken);

                    var buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
                    try
                    {
                        await using var sourceStream = new FileStream(
                            source,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite,
                            BUFFER_SIZE,
                            FileOptions.SequentialScan | FileOptions.Asynchronous);

                        await using var tempStream = new FileStream(
                            tempDest,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            BUFFER_SIZE,
                            FileOptions.Asynchronous);

                        int bytesRead;
                        while((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                        {
                            await tempStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        }

                        await tempStream.FlushAsync(cancellationToken);
                    } finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    File.Move(tempDest, dest, overwrite: true);
                } finally
                {
                    try
                    {
                        if(File.Exists(tempDest))
                            File.Delete(tempDest);
                    } catch
                    {
                    }
                }
            
        }

        private async Task TryReleaseFileLocksAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(LOCK_RETRY_DELAY, cancellationToken);

                try
                {
                    await using var testStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite,
                        1,
                        FileOptions.Asynchronous);
                } catch(IOException)
                {
                    await Task.Delay(LOCK_RETRY_DELAY * 2, cancellationToken);
                }
            } catch
            {
                SqliteConnection.ClearAllPools();
                await Task.Delay(100, cancellationToken);
            }
        }


        public DatabaseBackupType DetermineBackupType(string fileName)
        {
            if(string.IsNullOrEmpty(fileName))
                return DatabaseBackupType.Manual;

            var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();

            if(name.Contains("safety") || name.Contains("security") || name.Contains("before_restore"))
                return DatabaseBackupType.Safety;
            if(name.Contains("auto") || name.Contains("scheduled") || name.Contains("cron"))
                return DatabaseBackupType.Automatic;
            if(name.Contains("migration") || name.Contains("mig") || name.Contains("upgrade"))
                return DatabaseBackupType.Migration;
            if(name.Contains("system") || name.Contains("sys") || name.Contains("internal"))
                return DatabaseBackupType.System;

            return DatabaseBackupType.Manual;
        }

        public async Task<int> CleanOldBackupsAsync(
            string backupDir,
            string databaseName,
            int keepLast = 10,
            CancellationToken cancellationToken = default)
        {
            if(!Directory.Exists(backupDir))
                return 0;

            try
            {
                var backups = Directory.GetFiles(backupDir, $"{databaseName}_*.backup")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Skip(keepLast)
                    .ToList();

                var deleteTasks = backups.Select(
                    async file =>
                    {
                        try
                        {
                            await Task.Run(() => file.Delete(), cancellationToken);
                            return 1;
                        } catch
                        {
                            return 0;
                        }
                    });

                var results = await Task.WhenAll(deleteTasks);
                return results.Sum();
            } catch(Exception ex)
            {
                _logger?.LogError(ex, "Backup temizleme başarısız: {Database}", databaseName);
                return 0;
            }
        }

        public bool IsValidBackupFile(string backupDir, string fileName)
        {
            try
            {
                var path = Path.Combine(backupDir, fileName);
                return File.Exists(path) &&
                    new FileInfo(path).Length > 1024 &&
                    Path.GetExtension(path).Equals(".backup", StringComparison.OrdinalIgnoreCase);
            } catch
            {
                return false;
            }
        }

        private async Task<IDisposable> AcquireFileLockAsync(string filePath, TimeSpan timeout, CancellationToken ct)
        {
            var lockFile = $"{filePath}.lock";
            var start = DateTime.UtcNow;

            while(DateTime.UtcNow - start < timeout)
            {
                try
                {
                    var stream = new FileStream(
                        lockFile,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        1,
                        FileOptions.DeleteOnClose);

                    return stream; // Dispose olunca lock kalkar
                } catch(IOException)
                {
                    await Task.Delay(100, ct);
                }
            }

            throw new TimeoutException($"Dosya kilitli: {Path.GetFileName(filePath)}");
        }
    }
}