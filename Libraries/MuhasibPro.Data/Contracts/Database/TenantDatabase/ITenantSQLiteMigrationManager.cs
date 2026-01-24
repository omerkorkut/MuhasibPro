using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLiteMigrationManager
    {
        Task<DatabaseMigrationExecutionResult> InitializeTenantDatabaseAsync(string databaseName);
        Task<DatabaseCreatingExecutionResult> CreateNewTenantDatabase(
            string databaseName);
        Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(string databaseName);
        Task<List<string>> GetTenantPendingMigrationsAsync(string databaseName);
        Task<string> GetTenantCurrentDatabaseVersionAsync(string databaseName);

    }
}
