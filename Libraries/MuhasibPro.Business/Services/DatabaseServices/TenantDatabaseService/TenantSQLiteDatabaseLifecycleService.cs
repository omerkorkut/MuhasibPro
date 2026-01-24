using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    public class TenantSQLiteDatabaseLifecycleService : ITenantSQLiteDatabaseLifecycleService
    {
        private readonly ITenantSQLiteDatabaseManager _databaseManager;
        private readonly ILogService _logService;
        private readonly IApplicationPaths _applicationPaths;

        public TenantSQLiteDatabaseLifecycleService(
            ITenantSQLiteDatabaseManager databaseManager,
            ILogService logService,
            IApplicationPaths applicationPaths)
        {
            _databaseManager = databaseManager;
            _logService = logService;
            _applicationPaths = applicationPaths;
        }    

        public async Task<ApiDataResponse<DatabaseCreatingExecutionResult>> CreateNewTenantDatabaseAsync(
            string databaseName)
        {
            var checkTenantDb = await ValidateTenantDatabaseAsync(databaseName);
            if(checkTenantDb.isValid)
            {
                var result = new DatabaseCreatingExecutionResult { DatabaseName = databaseName, };
                return new SuccessApiDataResponse<DatabaseCreatingExecutionResult>(
                    data: result,
                    message: "✅ Veritabanı zaten mevcut ve sağlıklı");
            }
            try
            {
                var createTenant = await _databaseManager.CreateNewTenantDatabaseAsync(databaseName);
                if(!createTenant.IsCreatedSuccess)
                {
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            "Mali Dönem Veritabanı İşlemleri",
                            "Veritabanı Oluşturma İşlemi",
                            "Veritabanı oluşturma işleminde hata",
                            $"Mali Döneme ait {databaseName} veritabanı oluşturulamadı");
                    return new ErrorApiDataResponse<DatabaseCreatingExecutionResult>(
                        data: createTenant,
                        message: createTenant.Message);
                }
                await _logService.SistemLogService
                    .SistemLogInformationAsync(
                        "Mali Dönem Veritabanı İşlemleri",
                        "Veritabanı Oluşturma İşlemi",
                        "Veritabanı başarıyla oluşturuldu",
                        $"Mali Döneme ait {databaseName} veritabanı başarıyla oluşturuldu");
                return new SuccessApiDataResponse<DatabaseCreatingExecutionResult>(
                    data: createTenant,
                    message: createTenant.Message);
            } catch(Exception ex)
            {
                await _logService.SistemLogService
                    .SistemLogExceptionAsync("Mali Dönem Veritabanı işlemleri", "Yeni Mali Dönem Veritabanı Oluştur", ex);
                return new ErrorApiDataResponse<DatabaseCreatingExecutionResult>(
                    null,
                    message: $"[HATA] Mali Dönem'e ait veritabanı oluşturalamadı : {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<DatabaseDeletingExecutionResult>> DeleteTenantDatabase(
            string databaseName)
        {
            var tenantFileExist = await ValidateTenantDatabaseAsync(databaseName);
            if(!tenantFileExist.isValid)
                return new ErrorApiDataResponse<DatabaseDeletingExecutionResult>(
                    data: null,
                    message: "⚠️ Silinecek veritabanı bulunamadı");
            try
            {
                var deleteTenant = await _databaseManager.DeleteTenantDatabase(databaseName);
                if(!deleteTenant.IsDeletedSuccess)
                {
                    await _logService.SistemLogService
                        .SistemLogErrorAsync(
                            "Mali Dönem Veritabanı İşlemleri",
                            "Veritabanı Silme İşlemi",
                            "Veritabanı silme işleminde hata",
                            $"Mali Döneme ait {databaseName} veritabanı silinemedi");
                    return new ErrorApiDataResponse<DatabaseDeletingExecutionResult>(
                        data: deleteTenant,
                        message: deleteTenant.Message);
                }
                await _logService.SistemLogService
                    .SistemLogInformationAsync(
                        "Mali Dönem Veritabanı İşlemleri",
                        "Veritabanı Silme İşlemi",
                        "Veritabanı başarıyla silindi",
                        $"Mali Döneme ait {databaseName} veritabanı silindi");
                return new SuccessApiDataResponse<DatabaseDeletingExecutionResult>(
                    data: deleteTenant,
                    deleteTenant.Message);
            } catch(Exception ex)
            {
                await _logService.SistemLogService
                    .SistemLogExceptionAsync("Mali Dönem Veritabanı işlemleri", "Mali Dönem Veritabanı Silme", ex);
                return new ErrorApiDataResponse<DatabaseDeletingExecutionResult>(
                    null,
                    message: $"[HATA] Mali Dönem'e ait veritabanı silenemedi : {ex.Message}");
                
            }
        }

        public ApiDataResponse<string> GenerateDatabaseName(string firmaKodu, int maliYil)
        {
            try
            {
                if (string.IsNullOrEmpty(firmaKodu))
                    return new ErrorApiDataResponse<string>(null, "⚠️ Firma Kodu boş olamaz!");
                if (maliYil < 2000 || maliYil > 2100)
                    return new ErrorApiDataResponse<string>(null, "⚠️ Geçersiz mali dönem yılı!");
                
                var databaseName = _applicationPaths.GenerateTenantDatabaseName("db-", firmaKodu, maliYil);
                if (_applicationPaths.TenantDatabaseFileExists(databaseName))
                {
                    return new ErrorApiDataResponse<string>(null, "❌ Bu veritabanı adı kullanılıyor, Veri güvenliği için işlem durduruldu"); ;
                }
                return new SuccessApiDataResponse<string>(databaseName, "✅ Veritabanı adı oluşturuldu");
            }
            catch (Exception ex)
            {                
                return new ErrorApiDataResponse<string>(
                    null,
                    message: $"[HATA] Veritabanı adı oluşturulamadı : {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(
            string databaseName)
        {
            var analysis = new DatabaseConnectionAnalysis();
            if (!string.IsNullOrEmpty(databaseName))
                return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis, message: "⚠️ Veritabanı adı boş olamaz");
            try
            {
                var analysisResult = await _databaseManager.GetTenantDatabaseStateAsync(databaseName);
                if(analysisResult == null)
                {
                    return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis, message: $"{databaseName}, Veritabanı analiz edilemedi");
                }
                if (analysisResult.HasError)
                {
                    await _logService.SistemLogService.SistemLogErrorAsync(
                        "Mali Dönem Veritabanı İşlemleri", 
                        "Veritabanı Analizi", 
                        "Analiz işleminde hata oluştu", 
                        analysis.Message);
                }
                return new SuccessApiDataResponse<DatabaseConnectionAnalysis>(data: analysisResult, message: analysisResult.Message);
            }
            catch (Exception ex)
            {
                await _logService.SistemLogService.SistemLogExceptionAsync("Mali Dönem Veritabanı işlemleri", "Veritabanı Analizi", ex);
                return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis, message: $"[HATA] {ex.Message}");
            }
        }

     

        public async Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                return (false, "⚠️ Veritabanı adı boş olamaz!");            
            return await _databaseManager.ValidateTenantDatabaseAsync(databaseName);
        }
      
    }
}
