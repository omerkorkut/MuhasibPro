using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Business.Extensions.SistemService.AppService;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Data.Contracts.Repository.SistemRepos;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.SistemServices.AppServices
{
    public class FirmaService : IFirmaService
    {
        private readonly IFirmaRepository _firmaRepository;
        private readonly IUnitOfWork<SistemDbContext> _unitOfWork;
        private readonly IBitmapToolsService _bitmapTools;
        private readonly ILogService _logService;
        private readonly IAuthenticationService _authenticationService;

        public FirmaService(
            IFirmaRepository firmaRepository,
            IUnitOfWork<SistemDbContext> unitOfWork,
            IBitmapToolsService bitmapTools,
            ILogService logService,
            IAuthenticationService authenticationService)
        {
            _firmaRepository = firmaRepository;
            _unitOfWork = unitOfWork;
            _bitmapTools = bitmapTools;
            _logService = logService;
            _authenticationService = authenticationService;
        }

        public async Task<ApiDataResponse<FirmaModel>> GetByFirmaIdAsync(long firmaId)
        {
            try
            {
                var item = await FirmaServiceExtensions.GetByFirmaIdAsync(_firmaRepository, _bitmapTools, firmaId);
                if(item.Data == null)
                {
                    return new ErrorApiDataResponse<FirmaModel>(data: null, message: item.Message);
                }

                return new SuccessApiDataResponse<FirmaModel>(item.Data, item.Message);
            } catch(Exception ex)
            {
                await LogException(nameof(GetByFirmaIdAsync), ex);
                return new ErrorApiDataResponse<FirmaModel>(data: null, message: $"[HATA]: {ex.Message}");
            }
        }


        public async Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarPageAsync(
            int skip,
            int take,
            DataRequest<Firma> request)
        {
            var models = new List<FirmaModel>();
            try
            {
                var items = await _firmaRepository.GetFirmalarAsync(skip, take, request);
                if(items == null && items.Count < 0)
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: models,
                        message: "🔴 Listelenecek veri bulunamadı!");
                }
                foreach(var item in items)
                {
                    var model = await FirmaServiceExtensions.CreateFirmaModelAsync(
                        source: item,
                        boolIncludeAllFields: false,
                        _bitmapTools);
                    if(model != null)
                        models.Add(model);
                }
                return new SuccessApiDataResponse<IList<FirmaModel>>(
                    data: models,
                    message: "✅ İşlem başarılı",
                    resultCount: models.Count);
            } catch(Exception ex)
            {
                await LogException(nameof(GetFirmalarPageAsync), ex);
                return new ErrorApiDataResponse<IList<FirmaModel>>(data: models, message: $"[HATA]: { ex.Message}");
            }
        }

        public async Task<bool> IsFirmaAnyAsync() => await _firmaRepository.IsFirmaAnyAsync();

        public async Task<ApiDataResponse<int>> GetFirmalarCountAsync(DataRequest<Firma> request)
        {
            try
            {
                var count = await _firmaRepository.GetFirmalarCountAsync(request);
                if(count == 0)
                {
                    return new ErrorApiDataResponse<int>(data: 0, message: "🔴 Sayılacak veri bulunamadı!");
                }
                return new SuccessApiDataResponse<int>(data: count, message: "✅ İşlem başarılı", resultCount: count);
            } catch(Exception ex)
            {
                await LogException(nameof(GetFirmalarCountAsync), ex);
                return new ErrorApiDataResponse<int>(data: 0, message: $"[HATA]: { ex.Message}");
            }
        }

        public async Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarWithUserId(
            DataRequest<Firma> request,
            long userId)
        {
            var currentUserId = _authenticationService.GetCurrentUserId;
            var models = new List<FirmaModel>();
            try
            {
                var firmaList = await _firmaRepository.GetFirmaKeysUserIdAsync(request, userId);
                if(firmaList == null || !firmaList.Any())
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: models,
                        message: "🔴 Listelenecek veri bulunamadı!");
                }
                if(currentUserId > 0 && currentUserId != userId)
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: models,
                        message: "❌ Başka bir kullanıcının firmalarına erişim izniniz yok.");
                }
                if(firmaList != null)
                {
                    foreach(var item in firmaList)
                    {
                        models.Add(
                            await FirmaServiceExtensions.CreateFirmaModelAsync(
                                item,
                                boolIncludeAllFields: true,
                                _bitmapTools));
                    }
                }
                return new SuccessApiDataResponse<IList<FirmaModel>>(data: models, message: "✅ İşlem başarılı");
            } catch(Exception ex)
            {
                await LogException(nameof(GetFirmalarWithUserId), ex);
                return new ErrorApiDataResponse<IList<FirmaModel>>(data: null, $"[HATA]: {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarWithMaliDonemler(DataRequest<Firma> request)
        {
            var models = new List<FirmaModel>();
            try
            {
                var firmalar = await _firmaRepository.GetFirmalarWithMaliDonemler(request);
                if(!firmalar.Any())
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: models,
                        message: "🔴 Listelenecek veri bulunamadı!");
                }
                models = firmalar.Select(
                    f => new FirmaModel
                    {
                        Id = f.Id,
                        FirmaKodu = f.FirmaKodu,
                        KisaUnvani = f.KisaUnvani,
                        TamUnvani = f.TamUnvani,
                        YetkiliKisi = f.YetkiliKisi,
                        Telefon1 = f.Telefon1,
                        KaydedenId = f.KaydedenId,
                        KayitTarihi = f.KayitTarihi,
                        GuncelleyenId = f.GuncelleyenId,
                        GuncellemeTarihi = f.GuncellemeTarihi,

                        MaliDonemler =
                            f.MaliDonemler != null
                                    ? f.MaliDonemler
                                        .Select(
                                            md => new MaliDonemModel
                                        {
                                            Id = md.Id,
                                            MaliYil = md.MaliYil,
                                            DatabaseName = md.DatabaseName,
                                            DatabaseType = md.DatabaseType,
                                            AktifMi = md.AktifMi,
                                            KayitTarihi = md.KayitTarihi,
                                            KaydedenId = md.KaydedenId
                                        })
                                        .ToList()
                                    : new List<MaliDonemModel>()
                    })
                    .ToList();
                return new SuccessApiDataResponse<IList<FirmaModel>>(
                    data: models,
                    message: "✅ Firmalar listelendi",
                    resultCount: models.Count);
            } catch(Exception ex)
            {
                await LogException(nameof(GetFirmalarWithMaliDonemler), ex);
                return new ErrorApiDataResponse<IList<FirmaModel>>(data:null, $"[HATA]: {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<int>> UpdateFirmaAsync(FirmaModel model,CancellationToken cancellationToken)
        {
            if(_authenticationService.GetCurrentUserId <= 0)
            {
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ Giriş yapmış bir kullanıcı bulunamadı!");
            }
            if (model == null)
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ İşlem yapılacak firma bilgisi boş olamaz!");

            long firmaId = model.Id;
            try
            {
                var firma = firmaId > 0 ? await _firmaRepository.GetByFirmaIdAsync(firmaId) : new Firma();
                if(firmaId == 0)
                {
                    firma.KaydedenId = _authenticationService.GetCurrentUserId;
                }
                if (firmaId > 0) 
                {
                    model.GuncelleyenId = _authenticationService.GetCurrentUserId;
                }
                FirmaServiceExtensions.UpdateFirmaModel(firma, model);
                await _firmaRepository.UpdateFirmaAsync(firma);

                var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
                if(result > 0)
                {
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            "Firma İşlemleri",
                            "Firma Ekle/Güncelle",
                            $"Firma {(firmaId > 0 ? "güncellendi" : "eklendi")}",
                            $"Etkilenen kayıt: {result}");
                    // Model'i güncel veriyle doldur
                    var updateFirma = await GetByFirmaIdAsync(firma.Id);
                    if(updateFirma.Success && updateFirma.Data != null)
                    {
                        model.Merge(updateFirma.Data);
                    }
                    return new SuccessApiDataResponse<int>(
                        data: result,
                        message: $"✅ Firma {(firmaId > 0 ? "güncellendi" : "eklendi")}");
                }
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: "❌ Firma ekleme/güncelleme işlemi başarısız oldu!");
            } catch(Exception ex)
            {
                await LogException(nameof(UpdateFirmaAsync), ex);
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: $"[HATA] ❌ Firma ekleme/güncelleme işlemi başarız oldu! => { ex.Message}");
            }
        }

        public async Task<ApiDataResponse<int>> DeleteFirmaAsync(long firmaId,CancellationToken cancellationToken)
        {
            if(_authenticationService.GetCurrentUserId <= 0)
            {
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ Giriş yapmış bir kullanıcı bulunamadı!");
            }
            if (firmaId <= 0 )
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ Silinecek firma bilgisi boş olamaz!");
            try
            {
                var firma = await _firmaRepository.GetByFirmaIdAsync(firmaId);
                if(firma == null)
                    return new ErrorApiDataResponse<int>(data: 0, message: "🔴 Silinecek firma bulunamadı!");
                
                await _firmaRepository.DeleteFirmalarAsync(firma);
                var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
                if(result > 0)
                {
                    await _logService.SistemLogService
                    .SistemLogInformationAsync(
                        nameof(FirmaService),
                        nameof(DeleteFirmaAsync),
                        $"Firma başarıyla silindi",
                        $"Etkilenen kayıt: {result}");
                    return new SuccessApiDataResponse<int>(result, "✅ Firma başarıyla silindi.", resultCount: result);
                }
                return new ErrorApiDataResponse<int>(data: 0, "❌ Firma silme işlemi başarısız oldu!");
            } catch(Exception ex)
            {
                await LogException(nameof(DeleteFirmaAsync), ex);
                return new ErrorApiDataResponse<int>(data: 0, message: $"[HATA] ❌ Firma silinemedi! => { ex.Message}");
            }
        }

        public async Task<ApiDataResponse<int>> DeleteFirmaRangeAsync(int index, int length, DataRequest<Firma> request,CancellationToken cancellationToken)
        {
            if(_authenticationService.GetCurrentUserId <= 0)
            {
                return new ErrorApiDataResponse<int>(data: 0, message: "❌ Giriş yapmış bir kullanıcı bulunamadı!");
            }
            try
            {
                var items = await _firmaRepository.GetFirmaKeysAsync(index, length, request);
                if(items == null || !items.Any())
                {
                    return new ErrorApiDataResponse<int>(data: 0, message: "❌ Silinecek firma bulunamadı!");
                }
                await _firmaRepository.DeleteRangeAsync(items.ToArray());
                var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _logService.SistemLogService
                    .SistemLogInformationAsync(
                        nameof(FirmaService),
                        nameof(DeleteFirmaRangeAsync),
                        $"{items.Count} adet firma başarıyla silindi. Index: {index}, Length: {length}",
                        $"Etkilenen kayıt: {result}");
                return new SuccessApiDataResponse<int>(
                    result,
                    $"{items.Count} adet firma başarıyla silindi",
                    resultCount: items.Count);
            } catch(Exception)
            {
                throw;
            }
        }

        private async Task LogException(string methodName, Exception ex)
        { await _logService.SistemLogService.SistemLogExceptionAsync(nameof(FirmaService), methodName, ex); }
    }
}
