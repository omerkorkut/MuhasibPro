using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLiteDatabaseManager
    {
        Task<DatabaseConnectionAnalysis> GetTenantDatabaseStateAsync(
             string databaseName,
             CancellationToken cancellationToken);
        Task<DatabaseCreatingExecutionResult> CreateNewTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken);
        Task<DatabaseMigrationExecutionResult> InitializeTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken);
        Task<DatabaseDeletingExecutionResult> DeleteTenantDatabase(
            string databaseName,
            CancellationToken cancellationToken);
        Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken);
        (bool tenantFileExist, bool tenantDbValid) CheckTenantDatabaseState(string databaseName);


    }
}
