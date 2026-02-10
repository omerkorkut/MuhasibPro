using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.Extensions
{
    public static  class DbContextDatabaseCreateExtensions
    {
        public static async Task<DatabaseCreatingExecutionResult> ExecuteCreatingDatabaseAsync(
          this DbContext context,
          string databaseName,
          ILogger logger = null,
          int commandTimeoutMinutes = 5)
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
                await context.Database.MigrateAsync().ConfigureAwait(false);
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
    }
}
