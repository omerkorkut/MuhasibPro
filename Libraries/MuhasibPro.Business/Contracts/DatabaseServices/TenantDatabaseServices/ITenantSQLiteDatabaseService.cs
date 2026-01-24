using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseService
    {
        Task<ApiDataResponse<TenantCreationResult>> CreateNewTenantDatabaseAsync(TenantCreationRequest request);
        Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabaseAsync(TenantDeletingRequest request);
        Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName);
        Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(string databaseName );
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(string databaseName);
   
        
    }
}
