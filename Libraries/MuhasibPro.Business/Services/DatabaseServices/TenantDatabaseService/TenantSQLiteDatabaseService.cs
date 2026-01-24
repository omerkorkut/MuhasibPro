using Microsoft.Extensions.Logging;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService.Common;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    public class TenantSQLiteDatabaseService : ITenantSQLiteDatabaseService
    {
        private readonly ILogService _logService;
        private readonly IMaliDonemService _maliDonemService;
        private readonly IFirmaService _firmaService;
        private readonly ILogger<TenantSQLiteDatabaseService> _logger;
        private readonly IApplicationPaths _applicationPaths;
        private readonly ITenantSQLiteCommonService _commonService;


        public TenantSQLiteDatabaseService(
            ILogService logService,
            IMaliDonemService maliDonemService,
            IFirmaService firmaService,
            ILogger<TenantSQLiteDatabaseService> logger,
            IUnitOfWork<SistemDbContext> unitOfWork,
            IApplicationPaths applicationPaths,
            ITenantSQLiteCommonService commonService)
        {
            _logService = logService;
            _maliDonemService = maliDonemService;
            _firmaService = firmaService;
            _logger = logger;

            _applicationPaths = applicationPaths;
            _commonService = commonService;
        }

        public async Task<ApiDataResponse<TenantCreationResult>> CreateNewTenantDatabaseAsync(
            TenantCreationRequest request)
        {
            _logger.LogInformation(
                "Mali Dönem Veribanı oluşturma işlemi başlatıldı - FirmaId: {FirmaId}, MaliYil: {MaliYil}",
                request.FirmaId,
                request.MaliYil);

            var result = new TenantCreationResult
            {
                FirmaId = request.FirmaId,
                MaliYil = request.MaliYil,
                DatabaseName = request.DatabaseName,
                CreateCompleted = false,
            };
            var saga = new TenantOperationSaga(_logger);
            if (request.FirmaId <= 0)
                return ApiDataExtensions.ErrorResponse(result, "Firma Id geçersiz");
            // Mali Yıl Validasyonu
            result.StartStep(TenantCreationStep.MaliYilGecerlilikKontrolu);

            var maliYilResponse = TenantHelperExtensions.ValidateMaliYil(request.MaliYil);
            if (!maliYilResponse.Success || !maliYilResponse.Data)
            {
                result.CompleteStep(CreationStepStatus.Hata, maliYilResponse.Message);
                result.MarkAsError(maliYilResponse.Message);
                return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: maliYilResponse.Message);
            }

            result.MaliYil = request.MaliYil;
            result.CompleteStep(CreationStepStatus.Tamamlandi, maliYilResponse.Message);


            result.StartStep(TenantCreationStep.MaliDonemZatenVarMiKontrolu);
            var maliDonemExist = await _maliDonemService.ValidateMaliDonemExistsAsync(request.FirmaId, result.MaliYil);
            if (!maliDonemExist.Success)
            {
                result.CompleteStep(CreationStepStatus.Uyari, maliDonemExist.Message);
                result.MarkAsError(maliDonemExist.Message);
                return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: maliDonemExist.Message);
            }
            result.CompleteStep(CreationStepStatus.Calisiyor, "Mali Dönem kontrolü tamamlandı, İşlem devam ediyor");

            result.StartStep(TenantCreationStep.IslemBaslatildi);

            try
            {
                // Firma Validasyon
                result.StartStep(TenantCreationStep.FirmaBilgileriKontrolu);

                var firmaResponse = await _firmaService.ValidateFirmaAsync(request.FirmaId);
                if (!firmaResponse.Success || firmaResponse.Data == null)
                {
                    result.CompleteStep(CreationStepStatus.Hata, firmaResponse.Message);
                    result.MarkAsError(firmaResponse.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: firmaResponse.Message);
                }

                result.FirmaId = firmaResponse.Data.Id;
                result.CompleteStep(CreationStepStatus.Tamamlandi, firmaResponse.Message);

                // Firma Kodu kontrolü ve Veritabanı adı oluşturma
                result.StartStep(TenantCreationStep.VeritabaniAdiOlusturuluyor);

                if (string.IsNullOrWhiteSpace(firmaResponse.Data.FirmaKodu))
                {
                    result.CompleteStep(CreationStepStatus.Hata, "Firma Kodu boş olamaz");
                    result.MarkAsError("Firma kodu bilgilerine ulaşılamadı");
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: "Firma Kodu bilgilerine ulaşılamadı");
                }

                var databaseNameResponse = _applicationPaths.GenerateDatabaseName(
                    firmaResponse.Data.FirmaKodu,
                    request.MaliYil);
                if (!databaseNameResponse.Success || string.IsNullOrWhiteSpace(databaseNameResponse.Data))
                {
                    result.CompleteStep(CreationStepStatus.Hata, databaseNameResponse.Message);
                    result.MarkAsError(databaseNameResponse.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: databaseNameResponse.Message);
                }

                result.DatabaseName = databaseNameResponse.Data;
                // Caller tarafında kullanılacak request.DatabaseName'i güncelle
                request.DatabaseName = result.DatabaseName;

                result.CompleteStep(CreationStepStatus.Tamamlandi, databaseNameResponse.Message);

                // Mali Dönem Kaydı
                result.StartStep(TenantCreationStep.MaliDonemKaydiOlusturuluyor);

                var maliDonem = await _commonService.MaliDonemSagaStep.CreateNewMaliDonemAsync(saga, request);
                if (!maliDonem.Success || maliDonem.Data == null)
                {
                    result.CompleteStep(CreationStepStatus.Hata, maliDonem.Message);
                    result.MarkAsError(maliDonem.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: maliDonem.Message);
                }

                result.MaliDonemId = maliDonem.Data.MaliDonemId;
                result.CompleteStep(CreationStepStatus.Tamamlandi, maliDonem.Message);

                // Veritabanı oluştur
                /// <summary>
                /// ileriye dönük bir property.  ViewModel tarafından otomatik true gönderilecek Veritabanı oluşturmak
                /// şimdilik zorunlu.
                /// </summary>
                if (request.AutoCreateDatabase)
                {
                    result.StartStep(TenantCreationStep.VeritabaniDosyasiOlusturuluyor);

                    var maliVeritabani = await _commonService.TenantDatabaseSagaStep
                        .CreateTenantDatabaseAsync(saga, result.DatabaseName);
                    if (!maliVeritabani.Success || maliVeritabani.Data == null)
                    {
                        result.CompleteStep(CreationStepStatus.Hata, maliVeritabani.Message);
                        result.MarkAsError(maliVeritabani.Message);
                        return new ErrorApiDataResponse<TenantCreationResult>(
                            data: result,
                            message: maliVeritabani.Message);
                    }

                    result.DatabaseCreated = true;
                    result.CompleteStep(CreationStepStatus.Tamamlandi, maliVeritabani.Message);
                }
                else
                {
                    _logger.LogInformation("Veritabanı oluşturma atlandı (AutoCreateDatabase=false)");
                    result.DatabaseCreated = false;
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            "Mali Dönem İşlemleri",
                            "Mali Dönem Veritabanı Oluştur",
                            "Veritabanı oluşturma işlemi kullanıcı tarafından atlandı",
                            "Veritabanı oluşturma işlemi atlandı, sadece Mali Dönem kaydı oluşturuldu.");
                }

                result.StartStep(TenantCreationStep.TumIslemlerTamamlandi);
                result.CreateCompleted = true;
                result.MarkAsSuccess($"✅ {firmaResponse.Data.KisaUnvani} - {result.MaliYil} mali dönemi oluşturuldu");
                if (request.AutoCreateDatabase)
                    result.CompleteStep(CreationStepStatus.Tamamlandi, "Veritabanı başarıyla oluşturuldu");

                return new SuccessApiDataResponse<TenantCreationResult>(
                    data: result,
                    message: $"✅ {firmaResponse.Data.KisaUnvani} - {result.MaliYil} mali dönemi oluşturuldu");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.MarkAsError($"Beklenmeyen hata: {ex.Message}");
                _logger.LogError(
                    ex,
                    "Mali Dönem oluşturma BAŞARISIZ - FirmaId: {FirmaId}, MaliYil: {MaliYil}",
                    request.FirmaId,
                    request.MaliYil);
                await _logService.SistemLogService
                    .SistemLogExceptionAsync("Mali Dönem İşlemleri", "Mali Dönem Oluşturma", ex);
                return new ErrorApiDataResponse<TenantCreationResult>(
                    result,
                    $"Mali Dönem oluşturma hatası: {ex.Message}");
            }
        }

        private const string maliDonemIDNull = "Mali Dönem Id boş olamaz";
        private const string databaseNameNull = "Veritabanı adı boş olamaz";

        public async Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabaseAsync(
            TenantDeletingRequest request)
        {
            _logger.LogInformation(
                "Veritabanı silme işlemi başlatıldı - MaliDonemId: {MaliDonemId}, DatabaseName: {DatabaseName} ",
                request.MaliDonemId,
                request.DatabaseName);
            var result = new TenantDeletingResult
            {
                MaliDonemId = request.MaliDonemId,
                DatabaseName = request.DatabaseName,
                BackupCreateCompleted = false,
                BackupDeleteCompleted = false,
                DeletedBackupCount = 0,
                IsCurrentTenantDeletingBeforeBackup = false,
                MaliDonemDeleted = false,
                DatabaseDeleted = false,
                DeleteCompleted = false,
            };
            result.StartStep(TenantDeletionStep.IslemBaslatildi);
            var saga = new TenantOperationSaga(_logger);
            if (request.MaliDonemId <= 0)
            {
                result.CompleteStep(DeletionStepStatus.Hata, maliDonemIDNull);
                result.MarkAsError($"Gönderilen Mali Dönem Id : '{request.MaliDonemId}' , {maliDonemIDNull} ");
                return ApiDataExtensions.ErrorResponse(result, maliDonemIDNull);
            }
            if (request.DatabaseName == null)
            {
                result.CompleteStep(DeletionStepStatus.Hata, databaseNameNull);
                result.MarkAsError($"Gönderilen veritabanı : '{request.DatabaseName}', {databaseNameNull} ");
                return ApiDataExtensions.ErrorResponse(result, databaseNameNull);
            }
            var maliDonemResponse = await _commonService.TenantSQLiteDatabaseSelectedDetailService
                .GetTenantDetailsAsync(request.MaliDonemId);
            if (!maliDonemResponse.Success || maliDonemResponse.Data.DatabaseName != request.DatabaseName)
            {
                var message = maliDonemResponse.Message;
                result.CompleteStep(DeletionStepStatus.Hata, message);
                result.MarkAsError(message);
                return ApiDataExtensions.ErrorResponse(result, message);
            }
            result.MaliDonemId = maliDonemResponse.Data.MaliDonemId;
            result.DatabaseName = maliDonemResponse.Data.DatabaseName;
            try
            {
                if (request.IsDeleteDatabase)
                {
                    result.StartStep(TenantDeletionStep.VeritabaniDosyasiSiliniyor);

                    var deletingDatabaseResponse = await _commonService.TenantDatabaseSagaStep
                        .DeleteTenantDatabaseAsync(saga, request);
                    if (!deletingDatabaseResponse.Success || !deletingDatabaseResponse.Data.DatabaseDeleted)
                    {
                        var message = deletingDatabaseResponse.Message;
                        result.CompleteStep(DeletionStepStatus.Hata, message);
                        result.MarkAsError(message);
                        return ApiDataExtensions.ErrorResponse(result, message);
                    }
                    var response = deletingDatabaseResponse.Data;
                    result.DatabaseDeleted = response.DatabaseDeleted;

                    if (request.IsCurrentTenantDeletingBeforeBackup)
                    {
                        result.StartStep(TenantDeletionStep.VeritabaniSilmedenOnceYedekAliniyor);
                        if (response.BackupCreateCompleted)
                        {
                            result.BackupCreateCompleted = response.BackupCreateCompleted;
                            result.IsCurrentTenantDeletingBeforeBackup = response.IsCurrentTenantDeletingBeforeBackup;
                            result.BackupFilePath = response.BackupFilePath;
                            result.CompleteStep(DeletionStepStatus.Tamamlandi, "Veritabanı başarıyla yedeklendi");
                            result.MarkAsSuccess("Veritabanı başarıyla yedeklendi");
                        }
                        else
                        {
                            result.BackupCreateCompleted = false;
                            result.IsCurrentTenantDeletingBeforeBackup = false;
                            result.BackupFilePath = null;
                            result.CompleteStep(DeletionStepStatus.Hata, "Veritabanı yedek alma işlemi başarısız");
                            result.MarkAsError(deletingDatabaseResponse.Message);
                        }
                    }
                    if (request.DeleteAllTenantBackup)
                    {
                        result.StartStep(TenantDeletionStep.VeritabaniYedekleriSiliniyor);
                        if (response.BackupDeleteCompleted)
                        {
                            result.BackupDeleteCompleted = response.BackupDeleteCompleted;
                            result.DeletedBackupCount = response.DeletedBackupCount;
                            result.DeletedBackupFiles = response.DeletedBackupFiles;
                            result.CompleteStep(DeletionStepStatus.Tamamlandi, "Veritabanı yedekleri başarıyla silindi");
                            result.MarkAsSuccess("Veritabanı yedekleri başarıyla silindi");
                        }
                        else
                        {
                            result.BackupDeleteCompleted = false;
                            result.DeletedBackupCount = 0;
                            result.DeletedBackupFiles = null;
                            result.CompleteStep(DeletionStepStatus.Hata, "Veritabanı yedekleri silme işlemi başarısız");
                            result.MarkAsError(deletingDatabaseResponse.Message);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Veritabanı silme işlemi atlandı (IsDeleteDatabase=false)");
                    result.DatabaseDeleted = false;
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            "Mali Dönem İşlemleri",
                            "Mali Dönem Veritabanı Silme",
                            "Veritabanı Silme işlemi kullanıcı tarafından atlandı",
                            "Veritabanı Silme işlemi atlandı, sadece Mali Dönem kaydı silinecek");
                    result.CompleteStep(
                        DeletionStepStatus.Uyari,
                        "Veritabanı silme işlemi kullanıcı tarafından atlandı");
                }
                if (request.IsDeleteMaliDonem)
                {
                    result.StartStep(TenantDeletionStep.MaliDonemKaydiSiliniyor);
                    var deleteMaliDonemRecord = await _commonService.MaliDonemSagaStep
                        .DeleteMaliDonemAsync(saga, request);
                    if (!deleteMaliDonemRecord.Success || !deleteMaliDonemRecord.Data.MaliDonemDeleted)
                    {
                        var message = deleteMaliDonemRecord.Message;
                        result.CompleteStep(DeletionStepStatus.Hata, message);
                        result.MarkAsError(message);
                        return ApiDataExtensions.ErrorResponse(deleteMaliDonemRecord.Data, message);
                    }
                    var response = deleteMaliDonemRecord.Data;
                    if (response.MaliDonemDeleted)
                    {
                        result.MaliDonemDeleted = true;
                    }
                }
                else
                {
                    await _logService.SistemLogService
                        .SistemLogInformationAsync(
                            "Mali Dönem İşlemleri",
                            "Mali Dönem Kaydı Silme",
                            "Mali Dönem Kaydı Silme işlemi kullanıcı tarafından atlandı",
                            "Mali Dönem Kaydı işlemi atlandı, sadece veritabanı silinecek");
                    result.CompleteStep(
                        DeletionStepStatus.Uyari,
                        "Mali Dönem kaydı silme işlemi kullanıcı tarafından atlandı");
                }

                result.StartStep(TenantDeletionStep.TumIslemlerTamamlandi);
                result.DeleteCompleted = true;

                string resultMessage = "";
                if (result.DatabaseDeleted) resultMessage += "Veritabanı başarıyla silindi.";
                if (result.MaliDonemDeleted) resultMessage += "Mali Dönem Kaydı başarıyla silindi.";
                result.MarkAsSuccess(resultMessage.Trim());
                return ApiDataExtensions.SuccessResponse(result, "Silme işlemi başarıyla tamamlandı");
            }
            catch (Exception ex)
            {
                result.MarkAsError($"Beklenmeyen hata: {ex.Message}");
                _logger.LogError(
                    ex,
                    "Mali Dönem Silme BAŞARISIZ - MaliDonemId: {MaliDonemId}, Veritabanı: {DatabaseName}",
                    request.MaliDonemId,
                    request.DatabaseName);
                await _logService.SistemLogService
                    .SistemLogExceptionAsync("Mali Dönem İşlemleri", "Mali Dönem Silme", ex);
                return ApiDataExtensions.ErrorResponse(result, $"Mali Dönem ve Veritabanı silme hatası: {ex.Message}");
            }
        }


        public Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(string databaseName) => _commonService.TenantSQLiteDatabaseLifecycleService
            .GetTenantDatabaseStateAsync(databaseName);

    

        public async Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(string databaseName) => await _commonService.TenantSQLiteSelectionService
            .SwitchTenantAsync(databaseName);

        public async Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(string databaseName) => await _commonService.TenantSQLiteDatabaseLifecycleService
            .ValidateTenantDatabaseAsync(databaseName);
    }
}

