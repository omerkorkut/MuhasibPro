using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLiteDatabaseManager
    {
        Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(
             string databaseName);
        Task<DatabaseCreatingExecutionResult> CreateNewTenantDatabaseAsync(
            string databaseName);
        Task<DatabaseMigrationExecutionResult> InitializeTenantDatabaseAsync(
            string databaseName);
        Task<DatabaseDeletingExecutionResult> DeleteTenantDatabase(
            string databaseName);
        Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName);   


    }
}
