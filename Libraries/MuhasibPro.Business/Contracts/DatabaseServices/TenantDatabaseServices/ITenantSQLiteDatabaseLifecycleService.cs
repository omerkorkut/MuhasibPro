using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseLifecycleService
    {
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(
             string databaseName);
        Task<ApiDataResponse<DatabaseCreatingExecutionResult>> CreateNewTenantDatabaseAsync(
            string databaseName);
        
        Task<ApiDataResponse<DatabaseDeletingExecutionResult>> DeleteTenantDatabase(
            string databaseName);
        ApiDataResponse<string> GenerateDatabaseName(string firmaKodu, int maliYil);
        Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(string databaseName);
        
    }
}
