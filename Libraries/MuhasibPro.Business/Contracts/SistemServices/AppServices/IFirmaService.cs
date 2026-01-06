using MuhasibPro.Business.EntityModel.SistemModel;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.SistemServices.AppServices
{
    public interface IFirmaService
    {
        Task<bool> IsFirma();
        Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarWithUserId(long userId);
        Task<ApiDataResponse<FirmaModel>> GetByFirmaIdAsync(long id);
        Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarAsync(int skip, int take, DataRequest<Firma> request);
        Task<int> GetFirmalarCountAsync(DataRequest<Firma> request);
        Task<ApiDataResponse<int>> UpdateFirmaAsync(FirmaModel model);
        Task<ApiDataResponse<int>> DeleteFirmaAsync(FirmaModel model);
        Task<ApiDataResponse<int>> DeleteFirmaRangeAsync(int index, int length, DataRequest<Firma> request);
        

    }
}
