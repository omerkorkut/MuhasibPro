using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common
{
    public interface ITenantDatabaseSagaStep
    {
        Task<ApiDataResponse<TenantCreationResult>> CreateTenantDatabaseAsync(
           TenantOperationSaga sagaStepCreateDatabase,
           string databaseName);
        Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabaseAsync(
            TenantOperationSaga sagaStepDeleteDatabase,
            TenantDeletingRequest request);
    }
}
