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

        private (bool tenantFileExist, bool tenantDbValid) CheckTenantDatabaseState(string databaseName) => _databaseManager.CheckTenantDatabaseState(
            databaseName);

        public async Task<ApiDataResponse<DatabaseCreatingExecutionResult>> CreateNewTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken)
        {
            var checkTenantDb = CheckTenantDatabaseState(databaseName);
            if(checkTenantDb.tenantFileExist && checkTenantDb.tenantDbValid)
            {
                var result = new DatabaseCreatingExecutionResult { DatabaseName = databaseName, };
                return new SuccessApiDataResponse<DatabaseCreatingExecutionResult>(
                    data: result,
                    message: "✅ Veritabanı zaten mevcut ve sağlıklı");
            }
            try
            {
                var createTenant = await _databaseManager.CreateNewTenantDatabaseAsync(databaseName, cancellationToken);
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
            string databaseName,
            CancellationToken cancellationToken)
        {
            var tenantFileExist = CheckTenantDatabaseState(databaseName);
            if(!tenantFileExist.tenantFileExist)
                return new ErrorApiDataResponse<DatabaseDeletingExecutionResult>(
                    data: null,
                    message: "⚠️ Silinecek veritabanı bulunamadı");
            try
            {
                var deleteTenant = await _databaseManager.DeleteTenantDatabase(databaseName, cancellationToken);
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
            string databaseName,
            CancellationToken cancellationToken)
        {
            var analysis = new DatabaseConnectionAnalysis();
            if (!string.IsNullOrEmpty(databaseName))
                return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis, message: "⚠️ Veritabanı adı boş olamaz");
            try
            {
                var analysisResult = await _databaseManager.GetTenantDatabaseStateAsync(databaseName,cancellationToken);
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

        public async Task<ApiDataResponse<DatabaseMigrationExecutionResult>> InitializeTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken)
        {
            var initializeDB = new DatabaseMigrationExecutionResult();
            if (string.IsNullOrEmpty(databaseName))
                return new ErrorApiDataResponse<DatabaseMigrationExecutionResult>(data: initializeDB, message: "⚠️ Veritabanı adı boş olamaz!");
            try
            {
                var initializeResult = await _databaseManager.InitializeTenantDatabaseAsync(databaseName, cancellationToken);
                if (initializeResult.HasError) 
                {
                    await _logService.SistemLogService.SistemLogErrorAsync(
                       "Mali Dönem Veritabanı İşlemleri",
                       "Veritabanı Hazırlama ve güncelleme",
                       "Veritabanı hazırlı işleminde hata oluştu",
                       initializeResult.Message);
                    return new ErrorApiDataResponse<DatabaseMigrationExecutionResult>(data: initializeDB, message: initializeResult.Message);
                }                
                return new SuccessApiDataResponse<DatabaseMigrationExecutionResult>(data: initializeResult, message: initializeResult.Message); ;
            }
            catch (Exception ex)
            {
                await _logService.SistemLogService.SistemLogExceptionAsync("Mali Dönem Veritabanı işlemleri", "Veritabanı Hazırlama", ex);
                return new ErrorApiDataResponse<DatabaseMigrationExecutionResult>(data: initializeDB, message: $"[HATA] {ex.Message}");
            }
        }

        public async Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(databaseName))
                return (false, "⚠️ Veritabanı adı boş olamaz!");
            var check = CheckTenantDatabaseState(databaseName);
            if (!check.tenantFileExist)
                return (false, "🗃️ Veritabanı dosyası bulunamadı!");
            return await _databaseManager.ValidateTenantDatabaseAsync(databaseName, cancellationToken);
        }
      
    }
}
