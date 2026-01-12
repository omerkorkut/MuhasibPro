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
        /// <param name="context">DbContext instance</param>
        /// <param name="databaseName">Database adı (loglama için)</param>
        /// <param name="tablesToCheck">Kontrol edilecek tablo listesi</param>
        /// <param name="logger">Opsiyonel logger</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Database analiz sonucu</returns>
        /// <remarks>
        /// Bu metod sadece okuma yapar, herhangi bir değişiklik yapmaz. Integrity check, bağlantı testi ve migration
        /// durumunu kontrol eder.
        /// </remarks>
        public static async Task<DatabaseConnectionAnalysis> GetConnectionFullStateAsync(
            this DbContext context,
            string databaseName,
            bool isDatabaseExists,
            bool databaseValid,
            string[] tablesToCheck,
            ILogger logger = null,
            CancellationToken ct = default)
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
                    // SQLite'ın dahili bütünlük kontrolü (Hızlıdır)
                    var integrity = await context.Database
                        .SqlQueryRaw<string>("PRAGMA integrity_check")
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);
                    if (integrity?.ToLower() != "ok")
                    {
                        analysis.Message = "Veritabanı bütünlük kontrolü başarısız.";
                        analysis.HasError = true;
                        return analysis;
                    }
                }
                // 1. CONNECTION TEST

                analysis.CanConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
                if (!analysis.CanConnect)
                {
                    logger?.LogDebug("Database'e bağlanılamadı: {Database}", databaseName);
                    return analysis;
                }
                // 2. TABLE ANALYSIS (İyileştirilmiş)
                int existingTableCount = 0;
                bool hasActualData = false;

                foreach (var tableName in tablesToCheck)
                {
                    if (!IsValidTableName(tableName))
                        continue;

                    if (await TableExistsAsync(context, tableName, ct).ConfigureAwait(false))
                    {
                        existingTableCount++;
                        // Sadece bir tabloda bile veri olması, yedek alınması için yeterlidir.
                        if (!hasActualData && await TableHasRowsSafeAsync(context, tableName, ct).ConfigureAwait(false))
                        {
                            hasActualData = true;
                        }
                    }
                }

                analysis.TableCount = existingTableCount;
                // Eğer kontrol edilen hiçbir tablo yoksa VEYA tablolar var ama içi boşsa IsEmpty = true
                analysis.IsEmptyDatabase = existingTableCount == 0 || !hasActualData;

                // 3. MIGRATION ANALYSIS
                var applied = (await context.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).ToList();
                var pending = (await context.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false)).ToList();

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
        /// // WRITE OPERATION - migration uygula var result = await dbContext.ExecuteMigrationsWithBackupCheckAsync(
        /// databaseName: "SistemDB", tablesToCheck: new[] { "Kullanicilar", "AppDbVersiyonlar" }, backupAction: async
        /// () => await _backupManager.CreateBackupAsync(ct), logger: _logger, commandTimeoutMinutes: 10,
        /// cancellationToken: ct);
        /// </summary>
        public static async Task<DatabaseMigrationExecutionResult> ExecuteMigrationsWithBackupCheckAsync(
            this DbContext context,
            string databaseName,
            bool isDatabaseExists,
            bool databaseValid,
            string[] tablesToCheck,
            Func<Task<bool>> restoreAction = null,
            Func<Task<bool>> backupAction = null,
            ILogger logger = null,
            int commandTimeoutMinutes = 5,
            CancellationToken ct = default)
        {
            var result = new DatabaseMigrationExecutionResult
            {
                DatabaseName = databaseName,
                OperationTime = DateTime.UtcNow,
                DatabaseValid = databaseValid,
                IsRolledBack = false,
            };

            try
            {
                // 1. ÖNCE ANALİZ ET
                var analysis = await context.GetConnectionFullStateAsync(
                    databaseName,
                    isDatabaseExists,
                    databaseValid,
                    tablesToCheck,
                    logger,
                    ct).ConfigureAwait(false);
                if (!analysis.IsDatabaseExists)
                {
                    result.HasError = true;
                    result.Message = analysis.ToUIFullMessage();
                    logger?.LogWarning(result.Message);
                    return result;
                }
                result.CanConnect = analysis.CanConnect;
                result.DatabaseValid = analysis.DatabaseValid;
                result.PendingMigrations = analysis.PendingMigrations;
                result.AppliedMigrationsCount = analysis.AppliedMigrationsCount;

                if (!analysis.CanConnect)
                {
                    result.Message = analysis.ToUIFullMessage();
                    logger?.LogWarning(result.Message);
                    return result;
                }

                if (!analysis.IsUpdateRequired)
                {
                    result.Message = analysis.ToUIFullMessage();
                    logger?.LogInformation("{Database}: {Message}", databaseName, result.Message);
                    return result;
                }
                // 2. DOSYA BOYUT KONTROLÜ 
                if (!analysis.DatabaseValid && restoreAction != null)
                {
                    bool restoreSuccess = false;
                    int retryCount = 0;

                    while (!restoreSuccess && retryCount < 2) // En fazla 2 kere dene
                    {
                        try
                        {
                            restoreSuccess = await restoreAction();
                            result.IsRolledBack = restoreSuccess;
                        }
                        catch
                        {
                            retryCount++;
                            await Task.Delay(1000, ct); // 1 saniye bekle
                        }
                    }

                    if (!restoreSuccess)
                    {
                        result.Message = "Veritabanı meşgul veya kilitli, Geri yüklenemediği için işlem durduruldu.";
                        return result;
                    }
                }
                // 3. BACKUP KONTROLÜ (İyileştirilmiş)
                if (analysis.ShouldTakeBackupBeforeMigration && backupAction != null)
                {
                    bool backupSuccess = false;
                    int retryCount = 0;

                    while (!backupSuccess && retryCount < 2) // En fazla 2 kere dene
                    {
                        try
                        {
                            backupSuccess = await backupAction();
                        }
                        catch
                        {
                            retryCount++;
                            await Task.Delay(1000, ct); // 1 saniye bekle
                        }
                    }

                    if (!backupSuccess)
                    {
                        result.Message = "Veritabanı meşgul veya kilitli, yedek alınamadığı için işlem durduruldu.";
                        return result;
                    }
                }

                // 4. MIGRATION UYGULA
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(commandTimeoutMinutes));
                await context.Database.MigrateAsync(ct).ConfigureAwait(false);

                // 5. SON DURUMU KONTROL ET
                var finalAnalysis = await context.GetConnectionFullStateAsync(
                    databaseName,
                    isDatabaseExists,
                    databaseValid,
                    tablesToCheck,
                    logger,
                    ct).ConfigureAwait(false);

                if (finalAnalysis.DatabaseValid == false && restoreAction != null)
                {
                    await restoreAction();
                }
                result.DatabaseValid = finalAnalysis.DatabaseValid;
                result.AppliedMigrationsCount = finalAnalysis.AppliedMigrationsCount;
                result.PendingMigrations = finalAnalysis.PendingMigrations;
                result.Message = $"Başarıyla migration uygulandı ({analysis.PendingMigrations.Count} migration)";

                logger?.LogInformation(
                "Migration tamamlandı: {Database} ({Count} migration)",
                databaseName,
                analysis.PendingMigrations.Count);
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"Migration başarısız: {ex.Message}";
                logger?.LogError(ex, "Migration başarısız: {Database}", databaseName);
            }

            return result;
        }
        #endregion

        #region Created Execution (Write Operation)

        /// <summary>
        /// // WRITE OPERATION - veritabanı oluşturur  var created = await dbContext.ExecuteCreatingAsync( databaseName:
        /// "SistemDB", logger: _logger, commandTimeoutMinutes: 10, cancellationToken: ct);
        /// </summary>
        public static async Task<DatabaseCreatingExecutionResult> ExecuteCreatingDatabaseAsync(
            this DbContext context,
            string databaseName,
            ILogger logger = null,
            int commandTimeoutMinutes = 5,
            CancellationToken ct = default)
        {
            var result = new DatabaseCreatingExecutionResult
            {
                DatabaseName = databaseName,
                OperationTime = DateTime.UtcNow
            };

            try
            {
                // 3. MIGRATION UYGULA
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(commandTimeoutMinutes));
                await context.Database.MigrateAsync(ct).ConfigureAwait(false);
                var canConnect = await context.Database.CanConnectAsync().ConfigureAwait(false);
                if (canConnect)
                {
                    result.IsCreatedSuccess = true;
                    result.CanConnect = canConnect;
                    result.Message = $"✅ Veritabanı başarıyla oluşturuldu";

                    logger?.LogInformation("Veritabanı oluşturma işlemi tamamlandı {DatabaseName})", databaseName);
                }
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"❌ Veritabanı oluşturma işlemi başarısız: {ex.Message}";
                logger?.LogError(ex, "Veritabanı oluşturma işlemi başarısız: {Database}", databaseName);
            }

            return result;
        }
        #endregion

        #region Private Helpers
        private static async Task<bool> TableExistsAsync(DbContext context, string tableName, CancellationToken ct)
        {
            try
            {
                var sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = {0}";
                var count = await context.Database
                    .SqlQueryRaw<int>(sql, tableName)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
                return count > 0;
            }
            catch (Exception)
            {
                // Loglama yapma sorumluluğu çağırana bırakılıyor; burada false dön
                return false;
            }
        }

        private static async Task<bool> TableHasRowsSafeAsync(DbContext context, string tableName, CancellationToken ct)
        {
            if (!IsValidTableName(tableName))
                return false;

            try
            {
                var sql = $"SELECT COUNT(*) FROM \"{tableName}\" LIMIT 1";
                var count = await context.Database.SqlQueryRaw<int>(sql).FirstOrDefaultAsync(ct).ConfigureAwait(false);
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

            // 20240115143000_Initial gibi bir format varsa
            if (lastMigration.Length >= 14 && long.TryParse(lastMigration.AsSpan(0, 14), out _))
            {
                var ts = lastMigration.AsSpan(0, 14);
                // Format: 2024.01.15.1430 (Yıl.Ay.Gün.SaatDakika)
                return $"{ts.Slice(0, 4)}.{ts.Slice(4, 2)}.{ts.Slice(6, 2)}.{ts.Slice(8, 4)}";
            }

            return $"1.0.0.{migrationList.Count}";
        }
        #endregion
    }
}


