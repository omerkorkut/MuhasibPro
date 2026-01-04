using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.DataContext;

namespace MuhasibPro.Data.Database.Common
{

    public class AppDbContextFactory : IAppDbContextFactory
    {
        private readonly ITenantSQLiteConnectionStringFactory _connectionStringFactory;
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger<AppDbContextFactory> _logger;

        public AppDbContextFactory(ITenantSQLiteConnectionStringFactory connectionStringFactory, IApplicationPaths applicationPaths, ILogger<AppDbContextFactory> logger)
        {
            _connectionStringFactory = connectionStringFactory;
            _applicationPaths = applicationPaths;
            _logger = logger;
        }

        private string GetTenantDatabaseFilePath(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name required", nameof(databaseName));
            return _applicationPaths.GetTenantDatabaseFilePath(databaseName);
        }
        private bool GetTenantDatabaseValid(string databaseName) => _applicationPaths.IsSqliteDatabaseFileValid(GetTenantDatabaseFilePath(databaseName));

        public AppDbContext CreateDbContext(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database adı boş olamaz", nameof(databaseName));

            var options = CreateDbContextOptions(databaseName);
            return new AppDbContext(options);
        }
        private DbContextOptions<AppDbContext> CreateDbContextOptions(string databaseName)
        {
            var connectionString = _connectionStringFactory.CreateConnectionString(databaseName);

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlite(
                connectionString,
                sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(30);
                });

#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
#endif

            return optionsBuilder.Options;
        }

        public async Task<(bool canConnect, string message)> TestDbContextConnectionAsync(string databaseName, CancellationToken cancellationToken = default)
        {            
            // 2. File check
            var dbPath = GetTenantDatabaseFilePath(databaseName);
            if (!File.Exists(dbPath))
            {                
                return (false,"Veritabanı dosyası bulunamadı");
            }
            // 2. Dosya boyutu & Sqlite header durumunu kontrol et
            var dbValid = GetTenantDatabaseValid(databaseName);
            if (!dbValid)
            {
                _logger?.LogWarning("Veritabanı dosyası Sqlite için doğrulanamadı: {DatabaseName}", databaseName);
                return (false, $"Veritabanı dosyası Sqlite için doğrulanamadı: {databaseName}");
            }
            try
            {
                var connectionStringResult = await _connectionStringFactory.ValidateConnectionStringAsync(databaseName, cancellationToken);

                if (!connectionStringResult.canConnect)
                {
                    return (false, $"Bağlantı hatası: {connectionStringResult.message}");
                }
                using var context = CreateDbContext(databaseName);
                var canConnect = await context.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    _logger?.LogWarning("Veritabanı ile bağlantı kurulamadı: {DatabaseName}", databaseName);
                    return (false, "⛓️‍💥 Veritabanı bağlantısı kurululamadı");
                }                
                return (true, "🔗 Veritabanı bağlantısı başarılı");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 14)
            {
                return (false, "🔴 Veritabanı dosyası açılamadı!");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
            {
                return (false, "⚠️ Bilinmeyen veritabanı dosyası!");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı başarısız: {DatabaseName}", databaseName);
                return (false, $"Veritabanı Bağlantı Hatası: {ex.Message}");
            }
        }
    }
}
