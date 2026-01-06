using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteInfoService
    {
        Task<ApiDataResponse<List<TenantSelectionModel>>> GetTenantsForSelectionAsync(long? firmaId = null, DataRequest<MaliDonem> request = null);
        Task<ApiDataResponse<TenantDetailsModel>> GetTenantDetailsAsync(long maliDonemId);
    }
}
