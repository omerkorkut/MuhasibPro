using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.SistemServices.AppServices
{
    public interface IFirmaService
    {
        Task<ApiDataResponse<FirmaModel>> GetByFirmaIdAsync(long firmaId);
        Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarPageAsync(
            int skip,
            int take,
            DataRequest<Firma> request);
        Task<bool> IsFirmaAnyAsync();
        Task<ApiDataResponse<int>> GetFirmalarCountAsync(DataRequest<Firma> request);
        Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarWithUserId(DataRequest<Firma> request, long userId);
        Task<ApiDataResponse<int>> UpdateFirmaAsync(FirmaModel model);
        Task<ApiDataResponse<int>> DeleteFirmaAsync(long firmaId);
        Task<ApiDataResponse<int>> DeleteFirmaRangeAsync(int index, int length, DataRequest<Firma> request);
        Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarWithMaliDonemler(DataRequest<Firma> request);

    }
}
