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
    public class MaliDonemService
    {
        private readonly IMaliDonemRepository _maliDonemRepository;
        private readonly ILogService _logService;
        private readonly IUnitOfWork<SistemDbContext> _unitOfWork;
        private readonly IAuthenticationService _authenticationService;
        private readonly IFirmaService _firmaService;
        private readonly IBitmapToolsService _bitmapToolsService;

        public MaliDonemService(
            IMaliDonemRepository maliDonemRepository,
            ILogService logService,
            IUnitOfWork<SistemDbContext> unitOfWork,
            IAuthenticationService authenticationService,
            IFirmaService firmaService,
            IBitmapToolsService bitmapToolsService)
        {
            _maliDonemRepository = maliDonemRepository;
            _logService = logService;
            _unitOfWork = unitOfWork;
            _authenticationService = authenticationService;
            _firmaService = firmaService;
            _bitmapToolsService = bitmapToolsService;
        }

        public async Task<ApiDataResponse<MaliDonemModel>> GetByMaliDonemIdAsync(long malidonemId)
        {
            try
            {
                var item = await MaliDonemServiceExtensions.GetByMaliDonemIdAsync(
                        _maliDonemRepository,
                        _bitmapToolsService,
                        malidonemId);
                if (item == null)
                    return new ErrorApiDataResponse<MaliDonemModel>(data: null, message: item.Message);
                return new SuccessApiDataResponse<MaliDonemModel>(item.Data, item.Message);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(nameof(GetByMaliDonemIdAsync), ex);
                return new ErrorApiDataResponse<MaliDonemModel>(data:null, message: ex.Message);
            }
        }

        public async Task<ApiDataResponse<IList<MaliDonemModel>>> GetMaliDonemlerPageAsync(
            int skip,
            int take,
            DataRequest<MaliDonem> request)
        {
            var models = new List<MaliDonemModel>();
            try
            {
                var items = await _maliDonemRepository.GetMaliDonemlerAsync(skip, take, request);
                if(items == null)
                {
                    return new ErrorApiDataResponse<IList<MaliDonemModel>>(
                        data: models,
                        message: "🔴 Listelenecek veri bulunamadı!");
                }
                foreach(var item in items)
                {
                    var model = await MaliDonemServiceExtensions.CreateMaliDonemModelAsync(
                        source: item,
                        boolIncludeAllFields: false,
                        _bitmapToolsService);
                    if(model != null)
                        models.Add(model);
                }
                return new SuccessApiDataResponse<IList<MaliDonemModel>>(
                    data: models,
                    message: "✅ İşlem başarılı",
                    resultCount: models.Count);
            } catch(Exception ex)
            {
                await LogExceptionAsync(nameof(GetMaliDonemlerPageAsync), ex);
                return new ErrorApiDataResponse<IList<MaliDonemModel>>(data: models, message: $"[HATA]: {ex.Message}");
            }
        }

        public async Task<bool> IsMaliDonemAnyAsync() => await _maliDonemRepository.IsMaliDonemAnyAsync();

        public async Task<ApiDataResponse<int>> GetMaliDonemlerCountAsync(DataRequest<MaliDonem> request)
        {
            try
            {
                var count = await _maliDonemRepository.GetMaliDonemlerCountAsync(request);
                if(count == 0)
                {
                    return new ErrorApiDataResponse<int>(data: 0, message: "🔴 Sayılacak veri bulunamadı!");
                }
                return new SuccessApiDataResponse<int>(data: count, message: "✅ İşlem başarılı");
            } catch(Exception ex)
            {
                await LogExceptionAsync(nameof(GetMaliDonemlerCountAsync), ex);
                return new ErrorApiDataResponse<int>(data: 0, message: $"[HATA]: {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<MaliDonemModel>> CreateNewMaliDonemForFirmaAsync(long firmaId)
        {
            try
            {
                var model = new MaliDonemModel { FirmaId = firmaId };

                if (firmaId > 0)
                {
                    var firmaResult = await _firmaService.GetByFirmaIdAsync(firmaId);
                    if (firmaResult != null && firmaResult.Success && firmaResult.Data != null)
                    {
                        model.FirmaId = firmaResult.Data.Id;
                        model.FirmaModel = firmaResult.Data;

                        // Opsiyonel: Firma bilgilerinden varsayılan değerler atayabilirsiniz
                        // model.DatabasePath = firmaResult.Data.DizinYolu;
                        // model.DatabaseType = firmaResult.Data.VarsayilanVeritabaniTipi;
                    }
                    else
                    {
                        return new ErrorApiDataResponse<MaliDonemModel>(
                            data: null,
                            message: $"🔴 Firma bulunamadı (ID: {firmaId})");
                    }
                }
                else
                {
                    return new ErrorApiDataResponse<MaliDonemModel>(
                        data: null,
                        message: "❌ Geçersiz firma ID");
                }

                return new SuccessApiDataResponse<MaliDonemModel>(
                    data: model,
                    message: "✅ Yeni mali dönem modeli oluşturuldu");
            }
            catch (Exception ex)
            {
                // Loglama yapılabilir
                return new ErrorApiDataResponse<MaliDonemModel>(
                    data: null,
                    message: $"❌ Mali dönem modeli oluşturulurken hata: {ex.Message}");
            }
        }



        public async Task<ApiDataResponse<int>> UpdateMaliDonemAsync(MaliDonemModel model)
        {
            if(_authenticationService.GetCurrentUserId <= 0)
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ İşlem yapan kullanıcı bilgisi alınamadı!");

            if(model == null)
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: "⚠️ İşlem yapılacak mali dönem bilgisi boş olamaz!");

            long maliDonemId = model.Id;
            try
            {
                var maliDonem = maliDonemId > 0
                    ? await _maliDonemRepository.GetByMaliDonemIdAsync(maliDonemId)
                    : new MaliDonem();
                if (maliDonemId == 0)
                    maliDonem.KaydedenId = _authenticationService.GetCurrentUserId;
                if (maliDonemId > 0)
                    model.GuncelleyenId = _authenticationService.GetCurrentUserId;

                MaliDonemServiceExtensions.UpdateMaliDonemModel(maliDonem, model);
                await _maliDonemRepository.UpdateMaliDonemAsync(maliDonem);

                var result = await _unitOfWork.SaveChangesAsync();
                if(result > 0)
                {
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            nameof(this.ToString),
                            nameof(UpdateMaliDonemAsync),
                            $"Mali Dönem {(maliDonemId > 0 ? "güncellendi" : "eklendi")}",
                            $"Etkilenen kayıt: {result}");

                    var modelUpdated = await GetByMaliDonemIdAsync(maliDonem.Id);
                    if(modelUpdated.Success && modelUpdated.Data != null)
                    {
                        model.Merge(modelUpdated.Data);
                    }

                    return new SuccessApiDataResponse<int>(
                        data: result,
                        message: $"✅ Mali Dönem {(maliDonemId > 0 ? "güncellendi" : "eklendi")}");
                }
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: "❌ Mali Dönem ekleme/güncelleme işlemi başarısız oldu!");
            } catch(Exception ex)
            {
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: $"[HATA] ❌ Mali Dönem ekleme/güncelleme işlemi başarısız oldu! => {ex.Message}");
                throw;
            }
        }

        public async Task<ApiDataResponse<int>> DeleteMaliDonemAsync(long maliDonemId)
        {
            if(_authenticationService.GetCurrentUserId <= 0)
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ İşlem yapan kullanıcı bilgisi alınamadı!");
            if(maliDonemId <= 0)
                return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ Silinecek mali dönem bilgisi boş olamaz!");
            try
            {
                var maliDonem = await _maliDonemRepository.GetByMaliDonemIdAsync(maliDonemId);
                if(maliDonem == null)
                    return new ErrorApiDataResponse<int>(data: 0, message: "⚠️ Mali Dönem bulunamadı!");

                await _maliDonemRepository.DeleteMaliDonemlerAsync(maliDonem);
                var result = await _unitOfWork.SaveChangesAsync();
                if(result > 0)
                {
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            nameof(this.ToString),
                            nameof(DeleteMaliDonemAsync),
                            "Mali Dönem silindi",
                            $"Etkilenen kayıt: {result}");
                    return new SuccessApiDataResponse<int>(data: result, message: "✅ Mali Dönem silme işlemi başarılı");
                }
                return new ErrorApiDataResponse<int>(data: 0, message: "❌ Mali Dönem silme işlemi başarısız oldu!");
            } catch(Exception ex)
            {
                await LogExceptionAsync(nameof(DeleteMaliDonemAsync), ex);
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: $"[HATA] ❌ Mali Dönem silme işlemi başarısız oldu! => {ex.Message}");
            }
        }

        private async Task LogExceptionAsync(string methodName, Exception ex)
        { await _logService.SistemLogService.SistemLogExceptionAsync(nameof(MaliDonemService), methodName, ex); }
    }
}
