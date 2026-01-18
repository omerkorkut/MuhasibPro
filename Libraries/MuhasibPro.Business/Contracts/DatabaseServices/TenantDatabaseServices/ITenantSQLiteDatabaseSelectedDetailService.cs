using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseSelectedDetailService
    {
        Task<ApiDataResponse<List<TenantSelectionModel>>> GetUserTenantsForSelectionAsync(
            DataRequest<Firma> request,
            long userId);
        Task<ApiDataResponse<TenantDetailsModel>> GetTenantDetailsAsync(long maliDonemId);
    }
}
