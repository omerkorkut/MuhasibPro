using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.DTOModel;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Data.Contracts.Repository.SistemRepos;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Extensions.SistemService.AppService
{
    public static class FirmaServiceExtensions
    {
        public static void UpdateFirmaModel(Firma target, FirmaModel source)
        {
            var firma = ModelFactory.UpdateEntityFromModel(target, source,
                (entity, model) =>
                {
                    entity.Adres = model.Adres;
                    entity.Eposta = model.Eposta;
                    entity.KisaUnvani = model.KisaUnvani;
                    entity.LogoOnizleme = model.LogoOnizleme;
                    entity.PostaKodu = model.PostaKodu;
                    entity.TamUnvani = model.TamUnvani;
                    entity.TCNo = model.TCNo;
                    entity.Telefon1 = model.Telefon1;
                    entity.YetkiliKisi = model.YetkiliKisi;
                    entity.Il = model.Il;
                    entity.Ilce = model.Ilce;
                    entity.Logo = model.Logo;
                    entity.PBu1 = model.PBu1;
                    entity.PBu2 = model.PBu2;
                    entity.Telefon2 = model.Telefon2;
                    entity.VergiDairesi = model.VergiDairesi;
                    entity.VergiNo = model.VergiNo;
                    entity.Web = model.Web;
                });
        }
        public async static Task<FirmaModel> CreateFirmaModelAsync(            
            Firma source,
            bool boolIncludeAllFields,
            IBitmapToolsService bitmapTools)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));
            try
            {
                var model = await ModelFactory.CreateModelFromEntityAsync<FirmaModel, Firma>(
                    source,                    
                    boolIncludeAllFields,
                    async (model, entity, include) =>
                    {
                        model.FirmaKodu = entity.FirmaKodu;
                        model.Adres = entity.Adres;
                        model.Eposta = entity.Eposta;
                        model.KisaUnvani = entity.KisaUnvani;
                        model.LogoOnizleme = entity.LogoOnizleme;
                        model.LogoOnizlemeSource = entity.LogoOnizleme != null
                            ? await bitmapTools.LoadBitmapAsync(entity.LogoOnizleme)
                            : null;
                        model.PostaKodu = entity.PostaKodu;
                        model.TamUnvani = entity.TamUnvani;
                        model.Telefon1 = entity.Telefon1;
                        model.TCNo = entity.TCNo;
                        model.YetkiliKisi = entity.YetkiliKisi;
                        if(include)
                        {
                            model.Logo = entity.Logo;
                            model.LogoSource = entity.Logo != null
                                ? await bitmapTools.LoadBitmapAsync(entity.Logo)
                                : null;
                            model.Il = entity.Il;
                            model.Ilce = entity.Ilce;
                            model.Telefon2 = entity.Telefon2;
                            model.VergiDairesi = entity.VergiDairesi;
                            model.VergiNo = entity.VergiNo;
                            model.Web = entity.Web;
                            model.PBu1 = entity.PBu1;
                            model.PBu2 = entity.PBu2;
                        }
                    });
                return model;
            } catch(Exception ex)
            {
                throw new Exception("FirmaModel oluşturulurken hata oluştu", ex);
            }
        }

        public static async Task<ApiDataResponse<FirmaModel>> GetByFirmaIdAsync(
            IFirmaRepository repository,
            IBitmapToolsService bitmapTools,
            long firmaId)
        {
            try
            {
                var item = await repository.GetByFirmaIdAsync(firmaId);
                if (item == null)
                {
                    return new ErrorApiDataResponse<FirmaModel>(data: null, message: "⚠️ Firma bulunamadı");
                }
                var model = await CreateFirmaModelAsync(source: item, boolIncludeAllFields: true, bitmapTools);
                return new SuccessApiDataResponse<FirmaModel>(data: model, message: "✅ İşlem başarılı");
            }
            catch (Exception ex)
            {
                return new ErrorApiDataResponse<FirmaModel>(data: null, message: $"[Hata]:{ex.Message}");
            }
        }
    }
}
