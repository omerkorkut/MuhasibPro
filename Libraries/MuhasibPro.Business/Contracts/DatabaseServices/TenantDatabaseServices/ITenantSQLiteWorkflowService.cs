using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteWorkflowService
    {
        Task<ApiDataResponse<TenantCreationResult>> CreateNewTenantAsync(TenantCreationRequest request);
        Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantCompleteAsync(TenantDeletingRequest request);
        Task<ApiDataResponse<bool>> ValidateConnectionAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(string databaseName);
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetHealthStatusAsync(string databaseName);
    }
}
