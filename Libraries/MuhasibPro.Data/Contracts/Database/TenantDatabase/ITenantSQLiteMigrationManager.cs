using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLiteMigrationManager
    {
        Task<DatabaseMigrationExecutionResult> InitializeTenantDatabaseAsync(string databaseName, CancellationToken cancellationToken);
        Task<DatabaseCreatingExecutionResult> CreateNewTenantDatabase(
            string databaseName,
            CancellationToken cancellationToken = default);
        Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(string databaseName, CancellationToken cancellationToken);
        Task<List<string>> GetTenantPendingMigrationsAsync(string databaseName, CancellationToken cancellationToken);
        Task<string> GetTenantCurrentDatabaseVersionAsync(string databaseName, CancellationToken cancellationToken);

    }
}
