using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Contracts.Repository.SistemRepos
{
    public interface IFirmaRepository : IRepository<Firma>
    {
        Task<Firma> GetByFirmaIdAsync(long id);
        Task<IList<Firma>> GetFirmaKeysAsync(int skip, int take, DataRequest<Firma> request);
        Task<IList<Firma>> GetFirmalarAsync(int skip, int take, DataRequest<Firma> request);
        Task<IList<Firma>> GetFirmaKeysUserIdAsync(DataRequest<Firma> request, long userId);
        Task<IList<Firma>> GetFirmalarWithMaliDonemler(DataRequest<Firma> request);
        Task<int> GetFirmalarCountAsync(DataRequest<Firma> request);
        Task DeleteFirmalarAsync(params Firma[] firmalar);
        Task UpdateFirmaAsync(Firma firma);
        Task<bool> IsFirmaAnyAsync();
        Task<string> GetYeniFirmaKodu(string customCode = null);
        
    }
}
