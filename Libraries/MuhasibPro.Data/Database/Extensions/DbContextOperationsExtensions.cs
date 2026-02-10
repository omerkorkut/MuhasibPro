using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.Extensions
{
    public static class DbContextOperationsExtensions
    {
        #region Connection Analysis (Read-Only)

        /// <summary>
        /// Database bağlantısını, migration durumunu ve yapısını analiz eder (READ ONLY)
        /// </summary>
        public static async Task<DatabaseConnectionAnalysis> GetConnectionFullStateAsync(
            this DbContext context,
            string databaseName,
            bool isDatabaseExists,
            bool databaseValid,
            string[] tablesToCheck,
            ILogger logger = null)
        {
            var analysis = new DatabaseConnectionAnalysis
            {
                DatabaseName = databaseName,
                TableCount = 0,
                DatabaseValid = databaseValid,
                OperationTime = DateTime.UtcNow,
                IsDatabaseExists = isDatabaseExists,
            };

            try
            {
                if (!analysis.IsDatabaseExists)
                {
                    analysis.Message = "Veritabanı dosyası bulunamadı.";
                    logger?.LogWarning("Veritabanı bulunamadı: {Database}", databaseName);
                    return analysis;
                }

                if (!analysis.DatabaseValid)
                {
                    var integrity = await context.Database
                        .SqlQueryRaw<string>("PRAGMA integrity_check")
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);
                    if (integrity?.ToLower() != "ok")
                    {
                        analysis.Message = "Veritabanı bütünlük kontrolü başarısız.";
                        analysis.HasError = true;
                        return analysis;
                    }
                }

                // 1. CONNECTION TEST
                analysis.CanConnect = await context.Database.CanConnectAsync().ConfigureAwait(false);
                if (!analysis.CanConnect)
                {
                    logger?.LogDebug("Database'e bağlanılamadı: {Database}", databaseName);
                    return analysis;
                }

                // 2. TABLE ANALYSIS
                int existingTableCount = 0;
                bool hasActualData = false;

                foreach (var tableName in tablesToCheck)
                {
                    if (!IsValidTableName(tableName))
                        continue;

                    if (await TableExistsAsync(context, tableName).ConfigureAwait(false))
                    {
                        existingTableCount++;
                        if (!hasActualData && await TableHasRowsSafeAsync(context, tableName).ConfigureAwait(false))
                        {
                            hasActualData = true;
                        }
                    }
                }

                analysis.TableCount = existingTableCount;
                analysis.IsEmptyDatabase = existingTableCount == 0 || !hasActualData;

                // 3. MIGRATION ANALYSIS
                var applied = (await context.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).ToList();
                var pending = (await context.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToList();

                analysis.PendingMigrations = pending;
                analysis.AppliedMigrationsCount = applied.Count;
                analysis.CurrentVersion = GetVersionFromMigrations(applied);

                logger?.LogDebug(
                    "Database analizi tamamlandı: {Database}, Pending: {Count}",
                    databaseName,
                    analysis.PendingMigrations?.Count ?? 0);
            }
            catch (Exception ex)
            {
                analysis.HasError = true;
                analysis.Message = $"{databaseName} analiz edilemedi: {ex.Message}";
                logger?.LogError(ex, "Database analizi başarısız: {Db}", databaseName);
            }

            return analysis;
        }
        #endregion

        #region Migration Execution (Write Operation)

        /// <summary>
        /// Orijinal metod - geriye uyumluluk için
        /// </summary>
        public static async Task<DatabaseMigrationExecutionResult> ExecuteTenantMigrationsWithBackupCheckAsync(
            this DbContext context,
            string databaseName,
            bool isDatabaseExists,
            bool databaseValid,
            string[] tablesToCheck,
            Func<Task<bool>> restoreAction = null,
            Func<Task<bool>> backupAction = null,
            ILogger logger = null,
            int commandTimeoutMinutes = 5
            )
        {
            // İç analiz yap ve ana metoda yönlendir
            var analysis = await context.GetConnectionFullStateAsync(
                databaseName, isDatabaseExists, databaseValid, tablesToCheck, logger)
                .ConfigureAwait(false);

            return await context.ExecuteMigrationsWithBackupCheckAsync(
                analysis, restoreAction, backupAction, logger, commandTimeoutMinutes)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// ✅ YENİ: Analiz parametreli ana metod (TEMEL METOD)
        /// </summary>
        public static async Task<DatabaseMigrationExecutionResult> ExecuteMigrationsWithBackupCheckAsync(
            this DbContext context,
            DatabaseConnectionAnalysis analysis,
            Func<Task<bool>> restoreAction = null,
            Func<Task<bool>> backupAction = null,
            ILogger logger = null,
            int commandTimeoutMinutes = 5)
        {
            var result = new DatabaseMigrationExecutionResult
            {
                DatabaseName = analysis.DatabaseName,
                OperationTime = DateTime.UtcNow,
                DatabaseValid = analysis.DatabaseValid,
                IsRolledBack = false,
                CanConnect = analysis.CanConnect,
                PendingMigrations = analysis.PendingMigrations,
                AppliedMigrationsCount = analysis.AppliedMigrationsCount
            };

            try
            {
                // ✅ TEMEL KONTROLLER
                if (!analysis.IsDatabaseExists)
                {
                    result.HasError = true;
                    result.Message = analysis.ToUIFullMessage();
                    logger?.LogWarning(result.Message);
                    return result;
                }

                if (!analysis.CanConnect)
                {
                    result.HasError = true;
                    result.Message = analysis.ToUIFullMessage();
                    logger?.LogWarning(result.Message);
                    return result;
                }

                // ✅ 1. BACKUP KONTROLÜ (HER ZAMAN ÖNCE!)
                bool shouldTakeBackup = analysis.ShouldTakeBackupBeforeMigration;

                if (shouldTakeBackup && backupAction != null)
                {
                    logger?.LogInformation("Backup başlatılıyor: {Database}", analysis.DatabaseName);

                    bool backupSuccess = false;
                    int retryCount = 0;

                    while (!backupSuccess && retryCount < 2)
                    {
                        try
                        {
                            backupSuccess = await backupAction();
                            if (backupSuccess)
                            {
                                // Backup alındı bilgisini kaydet
                                result.BackupTaken = true;
                                result.Message = "Backup başarıyla alındı. ";
                                logger?.LogInformation("Backup başarılı: {Database}", analysis.DatabaseName);
                            }
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            logger?.LogWarning(ex, "Backup denemesi {Retry} başarısız", retryCount);
                            await Task.Delay(1000);
                        }
                    }

                    if (!backupSuccess)
                    {
                        result.HasError = true;
                        result.Message += "Yedek alınamadığı için işlem durduruldu.";
                        return result;
                    }
                }

                // ✅ 2. MIGRATION KONTROLÜ (BACKUP'TAN SONRA!)
                bool hasPendingMigrations = analysis.PendingMigrations?.Any() == true;

                // Migration gerekmiyorsa bile backup alınmış olabilir
                if (!hasPendingMigrations)
                {
                    // Eğer backup alındıysa mesaj zaten var
                    if (string.IsNullOrEmpty(result.Message))
                    {
                        result.Message = analysis.ToUIFullMessage();
                    }
                    else
                    {
                        // Backup mesajının üzerine ekle
                        result.Message += analysis.ToUIFullMessage();
                    }

                    logger?.LogInformation("{Database}: {Message}", analysis.DatabaseName, result.Message);
                    return result; // ✅ Migration gerekmiyor ama backup alınmış olabilir
                }

                // ✅ 3. VERİTABANI BÜTÜNLÜK KONTROLÜ
                if (!analysis.DatabaseValid && restoreAction != null)
                {
                    bool restoreSuccess = false;
                    int retryCount = 0;

                    while (!restoreSuccess && retryCount < 2)
                    {
                        try
                        {
                            restoreSuccess = await restoreAction();
                            result.IsRolledBack = restoreSuccess;
                        }
                        catch
                        {
                            retryCount++;
                            await Task.Delay(1000);
                        }
                    }

                    if (!restoreSuccess)
                    {
                        result.HasError = true;
                        result.Message += "Veritabanı meşgul veya kilitli, Geri yüklenemediği için işlem durduruldu.";
                        return result;
                    }
                }

                // ✅ 4. MIGRATION UYGULA
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(commandTimeoutMinutes));
                await context.Database.MigrateAsync().ConfigureAwait(false);

                // ✅ 5. SON DURUMU KONTROL ET
                // Tablo listesi analysis'ten alınabilir veya null geçilebilir
                var finalAnalysis = await context.GetConnectionFullStateAsync(
                    analysis.DatabaseName,
                    analysis.IsDatabaseExists,
                    analysis.DatabaseValid,
                    Array.Empty<string>(), // Tablo kontrolü gerekmiyor
                    logger)
                    .ConfigureAwait(false);

                // Migration sonrası hata kontrolü
                if (finalAnalysis.DatabaseValid == false && restoreAction != null)
                {
                    await restoreAction();
                    result.IsRolledBack = true;
                }

                result.DatabaseValid = finalAnalysis.DatabaseValid;
                result.AppliedMigrationsCount = finalAnalysis.AppliedMigrationsCount;
                result.PendingMigrations = finalAnalysis.PendingMigrations;

                // ✅ Modelin GetStatusMessage() metodunu kullan
                result.Message = result.ToUIFullMessage();

                // Backup alındıysa ek bilgi ver
                if (result.BackupTaken)
                {
                    result.Message += " (Yedek alındı)";
                }

                logger?.LogInformation(
                    "Migration tamamlandı: {Database} ({Count} migration, Backup: {Backup})",
                    analysis.DatabaseName,
                    analysis.PendingMigrations?.Count ?? 0,
                    result.BackupTaken ? "Evet" : "Hayır");
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"Migration başarısız: {ex.Message}";
                logger?.LogError(ex, "Migration başarısız: {Database}", analysis.DatabaseName);

                // Hata durumunda restore denenebilir
                if (restoreAction != null)
                {
                    try
                    {
                        await restoreAction();
                        result.IsRolledBack = true;
                        result.Message += " Hata nedeniyle yedekten geri yüklendi.";
                    }
                    catch
                    {
                        // Restore da başarısız olursa logla
                        logger?.LogError("Hata durumunda restore de başarısız oldu.");
                    }
                }
            }

            return result;
        }
        #endregion

        #region Private Helpers
        private static async Task<bool> TableExistsAsync(DbContext context, string tableName)
        {
            try
            {
                var sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = {0}";
                var count = await context.Database
                    .SqlQueryRaw<int>(sql, tableName)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                return count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<bool> TableHasRowsSafeAsync(DbContext context, string tableName)
        {
            if (!IsValidTableName(tableName))
                return false;

            try
            {
                var sql = $"SELECT COUNT(*) FROM \"{tableName}\" LIMIT 1";
                var count = await context.Database.SqlQueryRaw<int>(sql).FirstOrDefaultAsync().ConfigureAwait(false);
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;
            return tableName.All(c => char.IsLetterOrDigit(c) || c == '_') && char.IsLetter(tableName[0]);
        }

        private static string GetVersionFromMigrations(IEnumerable<string> migrations)
        {
            var migrationList = migrations.ToList();
            if (!migrationList.Any())
                return "1.0.0.0";

            var lastMigration = migrationList.Last();

            if (lastMigration.Length >= 14 && long.TryParse(lastMigration.AsSpan(0, 14), out _))
            {
                var ts = lastMigration.AsSpan(0, 14);
                return $"{ts.Slice(0, 4)}.{ts.Slice(4, 2)}.{ts.Slice(6, 2)}.{ts.Slice(8, 4)}";
            }

            return $"1.0.0.{migrationList.Count}";
        }
        #endregion
    }
}