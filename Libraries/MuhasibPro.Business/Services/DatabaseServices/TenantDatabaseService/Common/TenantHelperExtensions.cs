using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService.Common
{
    public static class TenantHelperExtensions
    {
        public static ApiDataResponse<bool> ValidateMaliYil(this int maliYil)
        {
            if(maliYil <= 0 || maliYil < DateTime.Now.Year - 2 || maliYil > 2100)
            {
                string errorDetail = maliYil <= 0
                    ? "sıfır veya negatif"
                    : maliYil < DateTime.Now.Year - 2 ? "çok eski" : "çok ileri";

                return new ErrorApiDataResponse<bool>(
                    data: false,
                    message: $"Geçersiz mali yıl ({errorDetail}): {maliYil}");
            }

            return new SuccessApiDataResponse<bool>(data: true, message: "Mali yıl geçerli");
        }

        public static async Task<ApiDataResponse<bool>> ValidateMaliDonemExistsAsync(
            this IMaliDonemService maliDonemService,
            long firmaId,
            int maliYil) // ⭐ Request yerine parametre
        {
            try
            {
                var exists = await maliDonemService.IsMaliDonemExistsAsync(firmaId, maliYil);

                // ⭐ Daha açık: "Mevcut değilse true" yerine direk bool dön
                if(exists)
                    return new ErrorApiDataResponse<bool>(
                        data: false,
                        message: $"Bu firma için {maliYil} mali dönemi zaten mevcut");

                return new SuccessApiDataResponse<bool>(data: true, message: "Mali dönem mevcut değil");
            } catch(Exception ex)
            {
                return new ErrorApiDataResponse<bool>(data: false, message: $"Mali dönem kontrol hatası: {ex.Message}");
            }
        }

        public static ApiDataResponse<string> GenerateDatabaseName(
            this IApplicationPaths applicationPaths,
            string firmaKodu,
            int maliYil)
        {
            if (applicationPaths == null) // ✅ Null check
                return new ErrorApiDataResponse<string>(null, "ApplicationPaths null");
            try
            {
                if(string.IsNullOrEmpty(firmaKodu))
                    return new ErrorApiDataResponse<string>(null, "Firma Kodu boş olamaz!");
                if(maliYil < 2000 || maliYil > 2100)
                    return new ErrorApiDataResponse<string>(null, "Geçersiz mali dönem yılı!");

                var databaseName = applicationPaths.GenerateTenantDatabaseName("db-", firmaKodu, maliYil);
                if(applicationPaths.TenantDatabaseFileExists(databaseName))
                {
                    return new ErrorApiDataResponse<string>(
                        null,
                        "Bu veritabanı adı kullanılıyor, Veri güvenliği için işlem durduruldu");
                    ;
                }
                return new SuccessApiDataResponse<string>(databaseName, "Veritabanı adı oluşturuldu");
            } catch(Exception ex)
            {
                return new ErrorApiDataResponse<string>(
                    null,
                    message: $"[HATA] Veritabanı adı oluşturulamadı : {ex.Message}");
            }
        }

        public static async Task<ApiDataResponse<FirmaModel>> ValidateFirmaAsync(
            this IFirmaService firmaService,
            long firmaId)
        {
            var firmaModel = new FirmaModel { Id = firmaId };

            try
            {
                if(firmaId <= 0)
                {
                    return new ErrorApiDataResponse<FirmaModel>(
                        data: firmaModel,
                        message: "Firma ID boş veya geçersiz olamaz!");
                }

                var firma = await firmaService.GetByFirmaIdAsync(firmaId: firmaId);
                if(!firma.Success || firma.Data == null)
                {
                    return new ErrorApiDataResponse<FirmaModel>(data: firmaModel, message: firma.Message);
                }
                firmaModel = firma.Data;
                return new SuccessApiDataResponse<FirmaModel>(data: firmaModel, message: firma.Message);
            } catch(Exception ex)
            {
                return new ErrorApiDataResponse<FirmaModel>(
                    data: firmaModel,
                    message: $"[HATA] Firma doğrulanırken hata oluştu : {ex.Message}");
            }
        }

    }
}
