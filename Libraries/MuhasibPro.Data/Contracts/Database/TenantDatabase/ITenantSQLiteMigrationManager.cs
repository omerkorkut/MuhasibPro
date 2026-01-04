using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace Muhasib.Data.Managers.DatabaseManager.Contracts.TenantDatabaseManager
{
    public interface ITenantSQLiteMigrationManager
    {
        Task<bool> InitializeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<List<string>> GetTenantPendingMigrationsAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<string> GetTenantCurrentDatabaseVersionAsync(string databaseName, CancellationToken cancellationToken = default);
     
    }
}
