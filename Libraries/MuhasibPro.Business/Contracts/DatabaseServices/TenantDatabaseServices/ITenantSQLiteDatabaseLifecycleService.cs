using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseLifecycleService
    {
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(
             string databaseName,
             CancellationToken cancellationToken);
        Task<ApiDataResponse<DatabaseCreatingExecutionResult>> CreateNewTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken);
        Task<ApiDataResponse<DatabaseMigrationExecutionResult>> InitializeTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken);
        Task<ApiDataResponse<DatabaseDeletingExecutionResult>> DeleteTenantDatabase(
            string databaseName,
            CancellationToken cancellationToken);
        ApiDataResponse<string> GenerateDatabaseName(string firmaKodu, int maliYil);
        Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(string databaseName, CancellationToken cancellationToken = default);
        
    }
}
