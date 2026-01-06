using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.EntityModel;
using MuhasibPro.Business.EntityModel.SistemModel;
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
    public class FirmaService
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
               await FirmaLogException(nameof(GetByFirmaIdAsync), ex);
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
                if(items == null)
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: models,
                        message: "🔴 Listelenecek veri bulunamadı!");
                }
                foreach(var item in items)
                {
                    var model = await FirmaServiceExtensions.CreateFirmaModelAsync(source: item, boolIncludeAllFields: false, _bitmapTools);
                    if(model != null)
                        models.Add(model);
                }
                return new SuccessApiDataResponse<IList<FirmaModel>>(
                    data: models,
                    message: "✅ İşlem başarılı",
                    resultCount: models.Count);
            } catch(Exception ex)
            {
                await FirmaLogException(nameof(GetFirmalarPageAsync), ex);
                return new ErrorApiDataResponse<IList<FirmaModel>>(data: models, message: $"[HATA]: { ex.Message}");
            }
        }

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
                await FirmaLogException(nameof(GetFirmalarCountAsync), ex);
                return new ErrorApiDataResponse<int>(data: 0, message: $"[HATA]: { ex.Message}");
            }
        }

        public async Task<ApiDataResponse<IList<FirmaModel>>> GetFirmalarWithUserId(DataRequest<Firma> request, long userId)
        {
            var currentUser = _authenticationService.CurrentAccount;
            var models = new List<FirmaModel>();
            try
            {
                var firmaList = await _firmaRepository.GetFirmaKeysWithUserIdAsync(request,userId);
                if(firmaList == null || !firmaList.Any())
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: null,
                        message: "🔴 Listelenecek veri bulunamadı!");
                }
                if(currentUser != null && currentUser.Id != userId)
                {
                    return new ErrorApiDataResponse<IList<FirmaModel>>(
                        data: null,
                        message: "❌ Başka bir kullanıcının firmalarına erişim izniniz yok.");
                }                
                if(firmaList != null)
                {
                    foreach(var item in firmaList)
                    {
                        models.Add(await FirmaServiceExtensions.CreateFirmaModelAsync(item, boolIncludeAllFields: true, _bitmapTools));
                    }
                }
                return new SuccessApiDataResponse<IList<FirmaModel>>(data: models, message: "✅ İşlem başarılı");
            } catch(Exception ex)
            {
                await FirmaLogException(nameof(GetFirmalarWithUserId), ex);
                return new ErrorApiDataResponse<IList<FirmaModel>>(data: null, $"[HATA]: {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<int>> UpdateFirmaAsync(FirmaModel model)
        {
            if(model == null)
                return new ErrorApiDataResponse<int>(data: 0, message: "❌ Güncellenecek firma bilgisi boş olamaz!");
            long firmaId = model.Id;
            try
            {
                var firma = firmaId > 0 ? await _firmaRepository.GetByFirmaIdAsync(firmaId) : new Firma();
                if(firma == null)
                {
                    return new ErrorApiDataResponse<int>(data: 0, message: "❌ Güncellenecek firma bulunamadı!");
                }                
                if (firmaId > 0)
                    firma.GuncelleyenId = _authenticationService.CurrentUserId;

                firma.KaydedenId = _authenticationService.CurrentUserId;
                FirmaServiceExtensions.UpdateFirmaModel(firma, model);
                await _firmaRepository.UpdateAsync(firma);

                var result = await _unitOfWork.SaveChangesAsync();
                
                await _logService.SistemLogService
                    .SistemLogInformation(
                        nameof(FirmaService),
                        nameof(UpdateFirmaAsync),
                        $"Firma başarıyla güncellendi. Firma ID: {firmaId}",
                        $"Etkilenen kayıt: {result}");
                // Model'i güncel veriyle doldur
                var updateFirma = await GetByFirmaIdAsync(firma.Id);
                if(updateFirma.Success)
                {
                    model.Merge(updateFirma.Data);
                }
                return new SuccessApiDataResponse<int>(
                    data: 1,
                    message: "✅ Firma bilgileri başarıyla güncellendi.",
                    resultCount: 1);
            } catch(Exception ex)
            {
                await FirmaLogException(nameof(UpdateFirmaAsync), ex);
                return new ErrorApiDataResponse<int>(data: 0, message: $"[HATA] ❌ Firma güncellenemedi! => { ex.Message}");
            }
        }
        public async Task<ApiDataResponse<int>> DeleteFirmaAsync(FirmaModel model)
        {
            if(model == null)
                return new ErrorApiDataResponse<int>(data: 0, message: "❌ Silinecek firma bilgisi boş olamaz!");
            try
            {                
                var firma = await _firmaRepository.GetByFirmaIdAsync(model.Id);
                if(firma == null)
                {
                    return new ErrorApiDataResponse<int>(data: 0, message: "❌ Silinecek firma bulunamadı!");
                }
                await _firmaRepository.DeleteFirmalarAsync(firma);
                var result = await _unitOfWork.SaveChangesAsync();
                await _logService.SistemLogService
                   .SistemLogInformation(
                       nameof(FirmaService),
                       nameof(DeleteFirmaAsync),
                       $"Firma başarıyla silindi. Firma ID: {model.Id}",
                       $"Etkilenen kayıt: {result}");
                return new SuccessApiDataResponse<int>(result, "✅ Firma başarıyla silindi.", resultCount:result);
            }
            catch (Exception ex)
            {
                await FirmaLogException(nameof(DeleteFirmaAsync), ex);
                return new ErrorApiDataResponse<int>(data: 0, message: $"[HATA] ❌ Firma silinemedi! => { ex.Message}");
            }
        }
        public async Task<ApiDataResponse<int>> DeleteFirmaRangeAsync(int index,int length,DataRequest<Firma> request)
        {
            try
            {
                var items = await _firmaRepository.GetFirmaKeysAsync(index, length, request);
                if(items == null || !items.Any())
                {
                    return new ErrorApiDataResponse<int>(data: 0, message: "❌ Silinecek firma bulunamadı!");
                }
                await _firmaRepository.DeleteRangeAsync(items.ToArray());
                var result = await _unitOfWork.SaveChangesAsync();
                await _logService.SistemLogService
                    .SistemLogInformation(
                        nameof(FirmaService),
                        nameof(DeleteFirmaRangeAsync),
                        $"{items.Count} adet firma başarıyla silindi. Index: {index}, Length: {length}",
                        $"Etkilenen kayıt: {result}");
                return new SuccessApiDataResponse<int>(result, $"{items.Count} adet firma başarıyla silindi",resultCount:items.Count);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private Task FirmaLogException(string methodName, Exception ex)
        {
            return _logService.SistemLogService
                .SistemLogException(nameof(FirmaService), methodName, ex);
        }
    }
}
