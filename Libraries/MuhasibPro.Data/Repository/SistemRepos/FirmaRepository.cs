using Microsoft.EntityFrameworkCore;
using MuhasibPro.Data.Contracts.Repository.SistemRepos;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Data.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities;
using MuhasibPro.Domain.Utilities.UIDGenerator;

namespace MuhasibPro.Data.Repository.SistemRepos
{
    public class FirmaRepository : BaseRepository<SistemDbContext, Firma>, IFirmaRepository
    {
        public FirmaRepository(SistemDbContext context) : base(context)
        {
        }
        public async Task DeleteFirmalarAsync(params Firma[] firmalar) { await base.DeleteRangeAsync(firmalar); }

        #region FirmaKod Oluştur
        public async Task<string> GetYeniFirmaKodu(string customCode = null)
        {
            var request = new CodeGenerationRequest
            {
                Prefix = "F-",
                DigitLength = 4,
                AllowCustomCode = true,
                FillGaps = true,
                CaseSensitive = false
            };

            return await CodeGenerator.GenerateCodeAsync(
                getExistingCodes: GetOlusturulanFirmaKodListe,
                request: request,
                customCode: customCode);
        }

        private async Task<IEnumerable<string>> GetOlusturulanFirmaKodListe()
        {
            return await DbSet
                .Where(f => !string.IsNullOrWhiteSpace(f.FirmaKodu))
                .Select(f => f.FirmaKodu)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
        }
        #endregion
        public async Task<Firma> GetByFirmaIdAsync(long id) 
        { return await DbSet.Where(a=> a.Id == id)
                .Include(a=> a.MaliDonemler)                
                .FirstOrDefaultAsync().
                ConfigureAwait(false); }

        public async Task<IList<Firma>> GetFirmaKeysUserIdAsync(DataRequest<Firma> request, long userId)
        {
            IQueryable<Firma> items = GetQuery(request);
            items.Include(r => r.MaliDonemler);
            items = items.Where(r => r.KaydedenId == userId);
            return await items                
                .Select(f => new Firma 
                {                    
                    Id = f.Id,
                    FirmaKodu = f.FirmaKodu,
                    KisaUnvani = f.KisaUnvani,
                    TamUnvani = f.TamUnvani,
                    YetkiliKisi = f.YetkiliKisi,
                    Telefon1 = f.Telefon1,
                    KayitTarihi = f.KayitTarihi,
                    KaydedenId = f.KaydedenId,
                    GuncellemeTarihi = f.GuncellemeTarihi,
                    GuncelleyenId = f.GuncelleyenId,
                    MaliDonemler = f.MaliDonemler
                                    .Where(md => md.AktifMi)
                                    .OrderByDescending(md => md.MaliYil)
                                    .Select(
                                        md => new MaliDonem
                                    {
                                        Id = md.Id,
                                        MaliYil = md.MaliYil,                                        
                                        DatabaseName = md.DatabaseName,
                                        DatabaseType = md.DatabaseType,
                                        KaydedenId = md.KaydedenId,
                                        KayitTarihi = md.KayitTarihi,
                                    })
                                    .ToList()
                })
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IList<Firma>> GetFirmalarWithMaliDonemler(DataRequest<Firma> request)
        {
            var query = GetQuery(request);
            query.Include(f => f.MaliDonemler);
            return await query
                .Select(
                    f => new Firma
                    {
                        Id = f.Id,
                        FirmaKodu = f.FirmaKodu,
                        KisaUnvani = f.KisaUnvani,
                        TamUnvani = f.TamUnvani,
                        YetkiliKisi = f.YetkiliKisi,
                        Telefon1 = f.Telefon1,
                        KayitTarihi = f.KayitTarihi,
                        KaydedenId = f.KaydedenId,
                        GuncellemeTarihi = f.GuncellemeTarihi,
                        GuncelleyenId = f.GuncelleyenId,
                        MaliDonemler =
                            f.MaliDonemler
                                    .Where(md => md.AktifMi)
                                    .OrderByDescending(md => md.MaliYil)
                                    .Select(
                                        md => new MaliDonem
                                    {
                                        Id = md.Id,
                                        MaliYil = md.MaliYil,                                        
                                        DatabaseName = md.DatabaseName,
                                        DatabaseType = md.DatabaseType,
                                        KaydedenId = md.KaydedenId,
                                        KayitTarihi = md.KayitTarihi,
                                    })
                                    .ToList()
                    })
                
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IList<Firma>> GetFirmaKeysAsync(int skip, int take, DataRequest<Firma> request)
        {
            IQueryable<Firma> items = GetQuery(request);
            items.Include(r => r.MaliDonemler);
            var records = await items.Skip(skip)
                .Take(take)
                .Select(r => new Firma { Id = r.Id })
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
            return records;
        }

        public async Task<IList<Firma>> GetFirmalarAsync(int skip, int take, DataRequest<Firma> request)
        {
            IQueryable<Firma> items = GetQuery(request);
            items.Include(r => r.MaliDonemler);
            var records = await items.Skip(skip)
                .Take(take)
                .Select(
                    r => new Firma
                    {
                        Adres = r.Adres,
                        AktifMi = r.AktifMi,
                        Eposta = r.Eposta,
                        GuncellemeTarihi = r.GuncellemeTarihi,
                        GuncelleyenId = r.GuncelleyenId,
                        Id = r.Id,
                        FirmaKodu = r.FirmaKodu,
                        Il = r.Il,
                        Ilce = r.Ilce,
                        KaydedenId = r.KaydedenId,
                        KayitTarihi = r.KayitTarihi,
                        KisaUnvani = r.KisaUnvani,
                        LogoOnizleme = r.LogoOnizleme,
                        PBu1 = r.PBu1,
                        PBu2 = r.PBu2,
                        PostaKodu = r.PostaKodu,
                        TamUnvani = r.TamUnvani,
                        TCNo = r.TCNo,
                        Telefon1 = r.Telefon1,
                        Telefon2 = r.Telefon2,
                        VergiDairesi = r.VergiDairesi,
                        VergiNo = r.VergiNo,
                        Web = r.Web,
                        YetkiliKisi = r.YetkiliKisi,
                    })
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
            return records;
        }

        public async Task<int> GetFirmalarCountAsync(DataRequest<Firma> request)
        {
            IQueryable<Firma> items = GetQuery(request);
            if(!string.IsNullOrEmpty(request.Query))
            {
                items.Where(r => r.ArananTerim.Contains(request.Query));
            }
            // Where
            if(request.Where != null)
            {
                items = items.Where(request.Where);
            }

            return await items.CountAsync().ConfigureAwait(false);
        }

        public async Task<bool> IsFirmaAnyAsync() { return await DbSet.AnyAsync().ConfigureAwait(false); }

        public async Task UpdateFirmaAsync(Firma firma)
        {
            if(firma.Id > 0)
            {
                firma.GuncellemeTarihi = DateTime.UtcNow;
                await UpdateAsync(firma);
            } else
            {
                firma.Id = UIDGenerator.GenerateModuleId(UIDModuleType.Sistem);
                firma.FirmaKodu = await GetYeniFirmaKodu();
                firma.KayitTarihi = DateTime.UtcNow;
                await AddAsync(firma);
            }
            firma.ArananTerim = firma.BuildSearchTerms();
        }
    }
}
