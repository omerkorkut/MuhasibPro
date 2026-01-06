using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Contracts.Repository.SistemRepos
{
    public interface IMaliDonemRepository : IRepository<MaliDonem>
    {
        Task<MaliDonem> GetByMaliDonemIdAsync(long id);
        Task<MaliDonem> GetByMaliDonemIdWithFirmaAsync(long id, long firmaId);
        MaliDonem GetByMaliDonemId(long id);
        Task<IList<MaliDonem>> GetMaliDonemlerAsync(int skip, int take, DataRequest<MaliDonem> request);
        Task<IList<MaliDonem>> GetMaliDonemKeysAsync(int skip, int take, DataRequest<MaliDonem> request);
        Task<int> GetMaliDonemlerCountAsync(DataRequest<MaliDonem> request);
        Task<bool> IsMaliDonem();
        Task DeleteMaliDonemlerAsync(params MaliDonem[] maliDonemler);
        Task UpdateMaliDonemAsync(MaliDonem maliDonem);


    }
}
