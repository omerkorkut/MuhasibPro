using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;
using System.Collections.Concurrent;

namespace MuhasibPro.Data.Database.Extensions
{
    public static class DbContextDiagnosticsExtensions
    {
        public static async Task<DatabaseHealtyDiagReport> GetDatabaseFullDiagStateAsync(
            this DbContext context,
            string databaseName,
            bool isDatabaseExists,
            bool databaseValid,
            string[] tablesToCheck,
            IProgress<AnalysisProgress> progressReporter = null,
            AnalysisOptions options = null,
            ILogger logger = null)
        {
            options ??= AnalysisOptions.Default;
            
            var analysis = new DatabaseHealtyDiagReport
            {
                DatabaseName = databaseName,
                TableCount = 0,
                DatabaseValid = databaseValid,
                OperationTime = DateTime.UtcNow,
                IsDatabaseExists = isDatabaseExists,
            };
            analysis.AnalysisProgress = progressReporter;
            try
            {
                // 1. BAŞLANGIÇ
                analysis.ReportProgress("Analiz başlatılıyor...", ProgressType.Info, 0, progressReporter);

                if (!analysis.IsDatabaseExists)
                {
                    var message = "Veritabanı dosyası bulunamadı.";
                    analysis.Message = message;
                    analysis.ReportProgress(message, ProgressType.Warning, 0, progressReporter);
                    return analysis;
                }

                // 2. BÜTÜNLÜK KONTROLÜ
                if (!analysis.DatabaseValid && options.CheckIntegrity)
                {
                    analysis.ReportProgress("Bütünlük kontrolü yapılıyor...", ProgressType.Info, 10, progressReporter);

                    var integrity = await context.Database
                        .SqlQueryRaw<string>("PRAGMA integrity_check")
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                    if (integrity?.ToLower() != "ok")
                    {
                        var message = "Veritabanı bütünlük kontrolü başarısız.";
                        analysis.Message = message;
                        analysis.HasError = true;
                        analysis.ReportProgress(message, ProgressType.Error, 0, progressReporter);
                        return analysis;
                    }
                    analysis.ReportProgress("✓ Bütünlük kontrolü başarılı", ProgressType.Success, 20, progressReporter);
                }

                // 3. BAĞLANTI TESTİ
                analysis.ReportProgress("Bağlantı test ediliyor...", ProgressType.Info, 30, progressReporter);

                analysis.CanConnect = await context.Database.CanConnectAsync().ConfigureAwait(false);
                if (!analysis.CanConnect)
                {
                    var message = "Veritabanına bağlanılamadı!";
                    analysis.ReportProgress(message, ProgressType.Error, 0, progressReporter);
                    return analysis;
                }
                analysis.ReportProgress("✓ Bağlantı başarılı", ProgressType.Success, 40, progressReporter);

                // 4. TABLO ANALİZİ (DÜZELTİLMİŞ VERSİYON)
                analysis.ReportProgress(
                    $"Tablo analizi başlatılıyor ({tablesToCheck.Length} tablo)...",
                    ProgressType.Info,
                    50,
                    progressReporter);

                int existingTableCount = 0;
                bool hasActualData = false;
                int processedTables = 0;

                // Geçerli tablo isimlerini filtrele
                var validTables = tablesToCheck
                    .Where(IsValidTableName)
                    .ToList();

                if (validTables.Count == 0)
                {
                    analysis.TableCount = 0;
                    analysis.IsEmptyDatabase = true;
                    analysis.ReportProgress("✓ Tablo analizi tamamlandı: Geçerli tablo ismi bulunamadı",
                        ProgressType.Success, 90, progressReporter);
                }
                else
                {
                    // Tüm tabloları önceden al (performans için)
                    var allTables = await GetAllTablesAsync(context).ConfigureAwait(false);

                    // Batch işlemleri için
                    var batches = validTables.Batch(options.BatchSize);

                    foreach (var batch in batches)
                    {
                        var batchList = batch.ToList();

                        // Batch için task listesi oluştur
                        var batchTasks = new List<Task<TableAnalysisResult>>();

                        foreach (var tableName in batchList)
                        {
                            batchTasks.Add(AnalyzeTableAsync(context, tableName, allTables, logger));
                        }

                        // Batch'i işle
                        var batchResults = await Task.WhenAll(batchTasks).ConfigureAwait(false);

                        // Sonuçları değerlendir
                        foreach (var result in batchResults)
                        {
                            if (result.Exists)
                            {
                                existingTableCount++;

                                if (result.HasRows)
                                {
                                    hasActualData = true;
                                }
                            }
                        }

                        // İlerleme güncelleme
                        processedTables += batchList.Count;
                        var progressPercentage = 50 + (int)((processedTables / (float)validTables.Count) * 40);

                        analysis.ReportProgress(
                            $"{processedTables}/{validTables.Count} tablo kontrol edildi...",
                            ProgressType.Info,
                            progressPercentage,
                            progressReporter);

                        // Her 10 tabloda bir veya sonunda detaylı rapor
                        if (processedTables % 10 == 0 || processedTables == validTables.Count)
                        {
                            var foundCount = batchResults.Count(r => r.Exists);
                            analysis.ReportProgress(
                                $"✓ {foundCount} tablo bulundu (Toplam: {existingTableCount})",
                                ProgressType.Success,
                                progressPercentage,
                                progressReporter);
                        }

                        // Bekleme (opsiyonel)
                        if (options.DelayBetweenBatches > TimeSpan.Zero)
                        {
                            await Task.Delay(options.DelayBetweenBatches).ConfigureAwait(false);
                        }
                    }

                    analysis.TableCount = existingTableCount;
                    analysis.IsEmptyDatabase = existingTableCount == 0 || !hasActualData;

                    analysis.ReportProgress(
                        $"✓ Tablo analizi tamamlandı: {existingTableCount} tablo, " +
                        $"{(hasActualData ? "veri içeriyor" : "boş")}",
                        ProgressType.Success,
                        90,
                        progressReporter);
                    
                }

                // 5. MIGRATION ANALİZİ
                if (options.CheckMigrations)
                {
                    analysis.ReportProgress("Migration kontrolü yapılıyor...", ProgressType.Info, 92, progressReporter);

                    var applied = (await context.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).ToList();
                    var pending = (await context.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToList();

                    analysis.PendingMigrations = pending;
                    analysis.AppliedMigrationsCount = applied.Count;
                    analysis.CurrentVersion = DatabaseUtilityExtensionsHelper.ExtractVersionFromMigration(applied.LastOrDefault());

                    analysis.ReportProgress(
                        $"✓ Migration analizi: {applied.Count} uygulanmış, {pending.Count} bekleyen",
                        ProgressType.Success,
                        95,
                        progressReporter);
                }

                analysis.ReportProgress("✓ Analiz tamamlandı!", ProgressType.Success, 100, progressReporter);

                logger?.LogDebug("Database analizi tamamlandı: {Database}", databaseName);
            }
            catch (Exception ex)
            {
                analysis.HasError = true;
                analysis.Message = $"{databaseName} analiz edilemedi: {ex.Message}";
                analysis.ReportProgress($"✗ HATA: {ex.Message}", ProgressType.Error, 0, progressReporter);
                logger?.LogError(ex, "Database analizi başarısız: {Db}", databaseName);
            }

            return analysis;
        }

      
        private static async Task<TableAnalysisResult> AnalyzeTableAsync(
            DbContext context,
            string tableName,
            List<string> allTables,
            ILogger logger)
        {
            var result = new TableAnalysisResult { TableName = tableName };

            try
            {
                // Önce cache'te kontrol (tüm tablolar listesinden)
                if (allTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                {
                    result.Exists = true;

                    // Tablo varsa satır kontrolü yap
                    result.HasRows = await TableHasRowsSafeAsync(context, tableName).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                logger?.LogWarning(ex, "Tablo analizi hatası: {Table}", tableName);
            }

            return result;
        }

        private static async Task<List<string>> GetAllTablesAsync(DbContext context)
        {
            try
            {
                var sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";

                return await context.Database
                    .SqlQueryRaw<string>(sql)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch
            {
                return new List<string>();
            }
        }        

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }

        // Tablo analizi sonuç modeli
        private class TableAnalysisResult
        {
            public string TableName { get; set; }
            public bool Exists { get; set; }
            public bool HasRows { get; set; }
            public string Error { get; set; }
        }

        // 1. IsValidTableName için:
        public static bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            if (tableName.Length > 64)
                return false;

            if (tableName.Contains("--") || tableName.Contains(";"))
                return false;

            if (tableName[0] != '_' && !char.IsLetter(tableName[0]))
                return false;

            foreach (char c in tableName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        // 2. TableExistsAsync için:
        public static async Task<bool> TableExistsAsync(
            DbContext context,
            string tableName)
        {
            if (!IsValidTableName(tableName))
                return false;

            try
            {
                var sql = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1";

                return await context.Database
                    .SqlQueryRaw<int>(sql, new SqliteParameter("@name", tableName))
                    .AnyAsync()
                    .ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        // 3. TableHasRowsSafeAsync için:
        public static async Task<bool> TableHasRowsSafeAsync(
            DbContext context,
            string tableName)
        {
            if (!IsValidTableName(tableName))
                return false;

            try
            {
                if (!await TableExistsAsync(context, tableName))
                    return false;

                var sql = $"SELECT 1 FROM \"{tableName.Replace("\"", "\"\"")}\" LIMIT 1";

                return await context.Database
                    .SqlQueryRaw<int>(sql)
                    .AnyAsync()
                    .ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        // 4. Cache versiyonu (opsiyonel, performans için)
        private static readonly ConcurrentDictionary<string, bool> _tableExistenceCache = new();

        public static async Task<bool> TableExistsCachedAsync(
            DbContext context,
            string tableName,
            bool useCache = true)
        {
            if (!IsValidTableName(tableName))
                return false;

            var cacheKey = $"{context.GetType().Name}:{tableName}";

            if (useCache && _tableExistenceCache.TryGetValue(cacheKey, out bool cached))
                return cached;

            var exists = await TableExistsAsync(context, tableName).ConfigureAwait(false);

            if (useCache && exists)
                _tableExistenceCache[cacheKey] = exists;

            return exists;
        }
    }

   
}