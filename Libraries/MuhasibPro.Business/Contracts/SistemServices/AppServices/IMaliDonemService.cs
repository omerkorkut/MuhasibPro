using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.SistemServices.AppServices
{
    public interface IMaliDonemService
    {
        Task<ApiDataResponse<MaliDonemModel>> GetByMaliDonemIdAsync(long id);
        Task<ApiDataResponse<IList<MaliDonemModel>>> GetMaliDonemlerPageAsync(int skip, int take, DataRequest<MaliDonem> request);
        
        Task<ApiDataResponse<int>> UpdateMaliDonemAsync(MaliDonemModel model);
        Task<ApiDataResponse<int>> DeleteMaliDonemAsync(long maliDonemId);
        Task<ApiDataResponse<int>> RestoreMaliDonemAsync(MaliDonemModel model);
        Task<ApiDataResponse<int>> GetMaliDonemlerCountAsync(DataRequest<MaliDonem> request);
        Task<bool> IsMaliDonemAnyAsync();
        Task<bool> IsMaliDonemExistsAsync(long firmaId, int maliYil);
        Task<ApiDataResponse<MaliDonemModel>> CreateNewMaliDonemForFirmaAsync(long firmaId);


    }
}
