using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    public class TenantSQLiteDatabaseSelectedDetailService : ITenantSQLiteDatabaseSelectedDetailService
    {
        private readonly IMaliDonemService _donemService;
        private readonly IFirmaService _firmaService;
        private readonly ILogService _logService;

        public TenantSQLiteDatabaseSelectedDetailService(IMaliDonemService donemService, ILogService logService, IFirmaService firmaService)
        {
            _donemService = donemService;
            _logService = logService;
            _firmaService = firmaService;
        }

        public async Task<ApiDataResponse<TenantDetailsModel>> GetTenantDetailsAsync(long maliDonemId)
        {
            var tenantDetails = new TenantDetailsModel { MaliDonemId = maliDonemId, };
            if(maliDonemId <= 0)
                return new ErrorApiDataResponse<TenantDetailsModel>(
                    data: tenantDetails,
                    message: "Mali Dönem ID boş veya geçersiz olamaz");
            try
            {
                var maliDonem = await _donemService.GetByMaliDonemIdAsync(maliDonemId);
                if(!maliDonem.Success && maliDonem.Data == null)
                {
                    return new ErrorApiDataResponse<TenantDetailsModel>(data: tenantDetails, message: maliDonem.Message);
                }
                if(maliDonem.Success && maliDonem.Data != null)
                {
                    var resultTenantDetail = new TenantDetailsModel
                    {
                        MaliDonemId = maliDonem.Data.Id,
                        DatabaseName = maliDonem.Data.DatabaseName,
                        MaliYil = maliDonem.Data.MaliYil,
                        UserId = maliDonem.Data.KaydedenId                    
                    };
                    if(maliDonem.Data.FirmaModel != null)
                    {
                        resultTenantDetail.FirmaId = maliDonem.Data.FirmaId;
                        resultTenantDetail.FirmaKodu = maliDonem.Data.FirmaModel.FirmaKodu;
                        resultTenantDetail.FirmaKisaUnvan = maliDonem.Data.FirmaModel.KisaUnvani;
                    }
                    return new SuccessApiDataResponse<TenantDetailsModel>(data: resultTenantDetail, message: "Mali Dönem'e ait veritabanı bilgileri alındı");
                }
                return new ErrorApiDataResponse<TenantDetailsModel>(data: tenantDetails, message: "Mali Dönem'e ait veritabanı bilgileri alınamadı");
            } catch(Exception ex)
            {
                await _logService.SistemLogService.SistemLogExceptionAsync("Mali Dönem Veritabanı Detay İşlemleri", "Mali Dönem Veritabanı İşlemleri", ex);
                return new ErrorApiDataResponse<TenantDetailsModel>(data: tenantDetails, message: $"HATA, { ex.Message }");
            }
        }

        public async Task<ApiDataResponse<List<TenantSelectionModel>>> GetUserTenantsForSelectionAsync(
    DataRequest<Firma> request,
    long userId)
        {
            if (userId <= 0)
            {
                return new ErrorApiDataResponse<List<TenantSelectionModel>>(
                    data: new List<TenantSelectionModel>(),
                    message: "Kullanıcı ID boş veya geçersiz olamaz");
            }

            try
            {
                // 1. Kullanıcının firmalarını al
                var userForFirmalar = await _firmaService.GetFirmalarWithUserId(request, userId);

                // DOĞRU NULL CHECK:
                if (!userForFirmalar.Success || userForFirmalar.Data == null)
                {
                    return new ErrorApiDataResponse<List<TenantSelectionModel>>(
                        data: new List<TenantSelectionModel>(),
                        message: userForFirmalar.Message ?? "Kullanıcı firmaları alınamadı");
                }

                if (!userForFirmalar.Data.Any())
                {
                    return new SuccessApiDataResponse<List<TenantSelectionModel>>(
                        data: new List<TenantSelectionModel>(),
                        message: "⚠️ Kullanıcının herhangi bir firması bulunamadı");
                }

                var tenantSelection = new List<TenantSelectionModel>();
                bool hasAnyMaliDonem = false;

                // 2. Her firma için mali dönemleri kontrol et
                foreach (var firma in userForFirmalar.Data)
                {
                    // Null check ekle
                    var maliDonemler = firma.MaliDonemler?
                        .Where(md => md != null && md.KaydedenId == userId)
                        .ToList() ?? new List<MaliDonemModel>();

                    if (!maliDonemler.Any())
                        continue; // Bu firmada mali dönem yok, devam et

                    hasAnyMaliDonem = true;

                    // 3. Her mali dönem için tenant model oluştur
                    foreach (var maliDonem in maliDonemler)
                    {
                        // Ek kontroller
                        if (string.IsNullOrWhiteSpace(maliDonem.DatabaseName))
                            continue;

                        tenantSelection.Add(new TenantSelectionModel
                        {
                            MaliDonemId = maliDonem.Id,
                            FirmaId = maliDonem.FirmaId,
                            FirmaKodu = firma.FirmaKodu ?? string.Empty, // Firma'dan al
                            FirmaKisaUnvani = firma.KisaUnvani ?? string.Empty,
                            MaliYil = maliDonem.MaliYil,
                            DatabaseName = maliDonem.DatabaseName ?? string.Empty,
                            DatabaseType = maliDonem.DatabaseType,
                            AktifMi = maliDonem.AktifMi,                            
                        });
                    }
                }

                // 4. Hiç mali dönem yoksa
                if (!hasAnyMaliDonem)
                {
                    return new SuccessApiDataResponse<List<TenantSelectionModel>>(
                        data: new List<TenantSelectionModel>(),
                        message: "ℹ️ Kullanıcının mali dönemi bulunamadı");
                }

                // 5. Sırala ve dön
                var sortedList = tenantSelection
                    .OrderByDescending(m => m.MaliYil)    // Sonra yıla göre                    
                    .ThenByDescending(m => m.AktifMi)    // Sonra aktif olanlar
                    .ToList();

                return new SuccessApiDataResponse<List<TenantSelectionModel>>(
                    data: sortedList,
                    message: $"✅ {sortedList.Count} mali dönem listelendi");
            }
            catch (Exception ex)
            {
                await _logService.SistemLogService.SistemLogExceptionAsync(
                    "Mali Dönem Veritabanı Liste İşlemleri",
                    "Mali Dönem Veritabanı İşlemleri",
                    ex);

                return new ErrorApiDataResponse<List<TenantSelectionModel>>(
                    data: new List<TenantSelectionModel>(),
                    message: $"❌ Hata oluştu: {ex.Message}");
            }
        }
    }
}
