using Microsoft.EntityFrameworkCore;
using MuhasibPro.Data.Contracts.Repository.SistemRepos;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Data.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.UIDGenerator;

namespace MuhasibPro.Data.Repository.SistemRepos
{
    public class MaliDonemRepository : BaseRepository<SistemDbContext, MaliDonem>, IMaliDonemRepository
    {
        public MaliDonemRepository(SistemDbContext context) : base(context)
        {
        }

        public async Task DeleteMaliDonemlerAsync(params MaliDonem[] maliDonemler)
        {
            await base.DeleteRangeAsync(maliDonemler);
        }

        public async Task<MaliDonem> GetByMaliDonemIdAsync(long id)
        {
            return await DbSet.Where(r => r.Id == id)
                .Include(r => r.Firma)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
        public async Task<MaliDonem> GetByMaliDonemIdWithFirmaAsync(long id, long firmaId)
        {
            return await DbSet.Where(r => r.Id == id && r.FirmaId == firmaId)
                .Include(r => r.Firma)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }   

        public async Task<IList<MaliDonem>> GetMaliDonemKeysAsync(int skip, int take, DataRequest<MaliDonem> request)
        {
            IQueryable<MaliDonem> items = GetQuery(request);
            items.Include(r => r.Firma);
            var record = await items.Skip(skip).Take(take)
                .Select(r => new MaliDonem
                {
                    Id = r.Id,
                    FirmaId = r.FirmaId,
                })
                .AsNoTracking()
                .ToListAsync();
            return record;
        }

        public async Task<IList<MaliDonem>> GetMaliDonemlerAsync(int skip, int take, DataRequest<MaliDonem> request)
        {
            IQueryable<MaliDonem> items = GetQuery(request);
            items.Include(r => r.Firma);
            // Execute
            var records = await items.Skip(skip).Take(take)                
                .AsNoTracking()
                .ToListAsync();

            return records;
        }

        public async Task<int> GetMaliDonemlerCountAsync(DataRequest<MaliDonem> request)
        {
            IQueryable<MaliDonem> items = GetQuery(request);
            items.Include(r => r.Firma);

            if (!string.IsNullOrEmpty(request.Query))
            {
                items.Where(r => r.ArananTerim.Contains(request.Query));
            }
            // Where
            if (request.Where != null)
            {
                items = items.Where(request.Where);
            }

            return await items.CountAsync();
        }

        public async Task<bool> IsMaliDonemAnyAsync()
        {
            return await DbSet.AnyAsync();
        }

        public async Task UpdateMaliDonemAsync(MaliDonem maliDonem)
        {
           
            if (maliDonem.Id > 0)
            {
                maliDonem.GuncellemeTarihi = DateTime.UtcNow;
                await UpdateAsync(maliDonem);
            }
            else
            {
                maliDonem.Id = UIDGenerator.GenerateModuleId(UIDModuleType.Sistem);
                maliDonem.KayitTarihi = DateTime.UtcNow;
                await AddAsync(maliDonem);
            }
            maliDonem.ArananTerim = maliDonem.BuildSearchTerms();
        }
        public async Task RestoreMaliDonemAsync(MaliDonem maliDonem)
        {
            if (maliDonem.Id > 0)
            {
                maliDonem.GuncellemeTarihi= DateTime.UtcNow;
                await AddAsync(maliDonem);
            }
        }
    }
}
