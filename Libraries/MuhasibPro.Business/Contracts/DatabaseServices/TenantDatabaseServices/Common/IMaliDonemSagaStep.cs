using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common
{
    public interface IMaliDonemSagaStep
    {
        Task<ApiDataResponse<TenantCreationResult>> CreateNewMaliDonemAsync(
            TenantOperationSaga sagaStepNewMaliDonem,
            TenantCreationRequest request);
        Task<ApiDataResponse<TenantDeletingResult>> DeleteMaliDonemAsync(
            TenantOperationSaga sagaStepDeleteMaliDonem,
            TenantDeletingRequest request);
    }
}
