using Microsoft.Extensions.Logging;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    public class TenantSQLiteDatabaseService : ITenantSQLiteDatabaseService
    {
        private readonly ITenantSQLiteDatabaseLifecycleService _lifecycleService;
        private readonly ILogService _logService;
        private readonly ITenantSQLiteSelectionService _selectionService;
        private readonly ITenantSQLiteDatabaseSelectedDetailService _selectedDetailService;
        private readonly IMaliDonemService _maliDonemService;
        private readonly IFirmaService _firmService;
        private readonly ILogger<TenantSQLiteDatabaseService> _logger;
        private readonly IUnitOfWork<SistemDbContext> _unitOfWork;
        private readonly IApplicationPaths _applicationPaths;

        public TenantSQLiteDatabaseService(
            ITenantSQLiteDatabaseLifecycleService lifecycleService,
            ILogService logService,
            ITenantSQLiteSelectionService selectionService,
            ITenantSQLiteDatabaseSelectedDetailService selectedDetailService,
            IMaliDonemService maliDonemService,
            IFirmaService firmService,
            ILogger<TenantSQLiteDatabaseService> logger,
            IUnitOfWork<SistemDbContext> unitOfWork,
            IApplicationPaths applicationPaths)
        {
            _lifecycleService = lifecycleService;
            _logService = logService;
            _selectionService = selectionService;
            _selectedDetailService = selectedDetailService;
            _maliDonemService = maliDonemService;
            _firmService = firmService;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _applicationPaths = applicationPaths;
        }

        public async Task<ApiDataResponse<TenantCreationResult>> CreateNewTenantDatabaseAsync(
            TenantCreationRequest request,CancellationToken cancellationToken)
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
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(10)); // 10 dakika timeout

            var linkedToken = timeoutCts.Token;
            var saga = new TenantOperationSaga(_logger);
            
            result.StartStep(TenantCreationStep.DuplicateKontrolu, "Mali Dönem kaydı kontrol ediliyor");
            var maliDonemExist = await ValidateMaliDonemExistsAsync(request,linkedToken);
            if (!maliDonemExist.Success)
            {
                result.CompleteStep(CreationStepStatus.Uyari, maliDonemExist.Message);
                result.MarkAsError(maliDonemExist.Message);
                return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: maliDonemExist.Message); ;
            }
            result.CompleteStep(CreationStepStatus.Calisiyor, "Mali Dönem kontrolü tamamlandı,İşlem devam ediyor");

            result.StartStep(
                TenantCreationStep.IslemBaslatildi,
                $"Mali Dönem ve veritabanı oluşturma işlemi başlatıldı: Mali Dönem :{request.MaliYil}");
            try
            {
                //Firma Validasyon
                result.StartStep(TenantCreationStep.FirmaValidasyonu, "Firma bilgileri kontrol ediliyor");

                var firmaResponse = await ValidateFirmaAsync(request.FirmaId);
                if(!firmaResponse.Success && firmaResponse.Data == null)
                {
                    result.CompleteStep(CreationStepStatus.Hata, firmaResponse.Message);
                    result.MarkAsError(firmaResponse.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: firmaResponse.Message);
                }
                result.FirmaId = firmaResponse.Data.Id;
                result.CompleteStep(CreationStepStatus.Tamamlandi, firmaResponse.Message);

                // Mali Yıl Validasyonu
                result.StartStep(TenantCreationStep.MaliYilValidasyonu, "Mali Yıl kontrol ediliyor");

                var maliYilResponse = IsValidMaliYil(request.MaliYil);
                if(!maliYilResponse.Success && maliYilResponse.Data <= 0)
                {
                    result.CompleteStep(CreationStepStatus.Hata, maliYilResponse.Message);
                    result.MarkAsError(maliYilResponse.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: maliYilResponse.Message);
                }
                result.MaliYil = maliYilResponse.Data;
                result.CompleteStep(CreationStepStatus.Tamamlandi, maliYilResponse.Message);

                //Firma Kodu kontrolü ve Veritabanı adı oluşturma
                result.StartStep(
                    TenantCreationStep.VeritabaniAdiOlusturuluyor,
                    "Mali Dönem'e ait veritabanı adı oluşturuluyor");

                if(firmaResponse.Data.FirmaKodu == null)
                {
                    result.CompleteStep(CreationStepStatus.Hata, "Firma Kodu boş olamaz");
                    result.MarkAsError("Firma kodu bilgilerine ulaşılamadı");
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: "Firma Kodu bilgilerine ulaşılamadı");
                }
                var databaseNameResponse = GenerateDatabaseName(firmaResponse.Data.FirmaKodu, request.MaliYil);
                if(!databaseNameResponse.Success && databaseNameResponse.Data == null)
                {
                    result.CompleteStep(CreationStepStatus.Hata, databaseNameResponse.Message);
                    result.MarkAsError(databaseNameResponse.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: databaseNameResponse.Message);
                }

                result.DatabaseName = databaseNameResponse.Data;

                result.CompleteStep(CreationStepStatus.Tamamlandi, databaseNameResponse.Message);

                // Mali Dönem Kaydı 
                result.StartStep(TenantCreationStep.MaliDonemKaydiOlusturuluyor, "Mali Dönem kaydı oluşturuluyor");

                var maliDonem = await YeniMaliDonemOlusturAsync(saga, request,result.DatabaseName, linkedToken);
                if(!maliDonem.Success && maliDonem.Data == null)
                {
                    result.CompleteStep(CreationStepStatus.Hata, maliDonem.Message);
                    result.MarkAsError(maliDonem.Message);
                    return new ErrorApiDataResponse<TenantCreationResult>(data: result, message: maliDonem.Message);
                }
                result.MaliDonemId = maliDonem.Data.MaliDonemId;
                result.CompleteStep(CreationStepStatus.Tamamlandi, maliDonem.Message);

                //Veritabanı oluştur
                /// <summary>
                /// ileriye dönük bir property.  ViewModel tarafından otomatik true gönderilecek Veritabanı oluşturmak
                /// şimdilik zorunlu.
                /// </summary>
                if(request.AutoCreateDatabase)
                {
                    result.StartStep(
                        TenantCreationStep.VeritabaniDosyasiOlusturuluyor,
                        "Mali Dönem'e ait veritabanı oluşturuluyor");

                    var maliVeritabani = await YeniMaliVeritabaniOlusturAsync(saga, request, linkedToken);
                    if(!maliVeritabani.Success && maliVeritabani.Data == null)
                    {
                        result.CompleteStep(CreationStepStatus.Hata, maliVeritabani.Message);
                        result.MarkAsError(maliVeritabani.Message);
                        return new ErrorApiDataResponse<TenantCreationResult>(
                            data: result,
                            message: maliVeritabani.Message);
                    }
                    result.DatabaseCreated = true;
                    result.CompleteStep(CreationStepStatus.Tamamlandi, maliVeritabani.Message);
                } else
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


                var dbCheck = _applicationPaths.TenantDatabaseFileExists(result.DatabaseName);
                if(request.AutoCreateDatabase && !dbCheck)
                {
                    try
                    {
                        result.StartStep(
                            TenantCreationStep.BeklenmeyenHata,
                            "Bilinmeyen hata, Tüm işlemler geri alınıyor");
                        result.MarkAsError("Tüm işlemler tamamlanamadı, Bilinmeyen bir hata oluştu");
                        result.CreateCompleted = false;
                        await saga.CompensateAllAsync(cancellationToken);
                        _logger.LogInformation("Tüm işlemler sıralı tamamlanamadı, Geri alma işlemi tamamlandı.");

                        result.StartStep(TenantCreationStep.IslemBaslatildi, "Geri alma işlemi başarıyla tamamlandı");
                        result.MarkAsSuccess("Tüm işlemler başarıyla geri alındı");
                        return new ErrorApiDataResponse<TenantCreationResult>(
                            data: result,
                            "Bilinmeyen bir hata oluştu, Tüm işlemler geri alındı");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        result.CreateCompleted = false;
                        result.MarkAsError(
                            "Geri alma işlmi sırasında hata oluştu.\n Sistem yöneticiniz ile görüşün.Elle müdahale gerekebilir");
                        _logger.LogError(
                            ex,
                            "Saga rollback sırasında hata oluştu! Manuel müdahale gerekebilir!");
                    }
                }

                result.StartStep(TenantCreationStep.TumIslemlerTamamlandi, "Tüm işlemler başarıyla tamamlandı");
                result.CreateCompleted = true;
                result.MarkAsSuccess($"✅ {firmaResponse.Data.KisaUnvani} - {result.MaliYil} mali dönemi oluşturuldu");
                if(request.AutoCreateDatabase)
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

        public Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabaseAsync(TenantDeletingRequest request)
        { throw new NotImplementedException(); }

        public Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(string databaseName, CancellationToken cancellationToken)
        => _lifecycleService.GetTenantDatabaseStateAsync(databaseName, cancellationToken);

        public Task<ApiDataResponse<DatabaseMigrationExecutionResult>> InitializeTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken)
        { throw new NotImplementedException(); }

        public async Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(
            string databaseName,
            CancellationToken cancellationToken)
        => await _selectionService.SwitchTenantAsync(databaseName, cancellationToken);

        public async Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken = default) => await _lifecycleService.ValidateTenantDatabaseAsync(
            databaseName,
            cancellationToken);

        #region Saga Step
        private async Task<ApiDataResponse<TenantCreationResult>> YeniMaliDonemOlusturAsync(
            TenantOperationSaga sagaStepMaliDonem,
            TenantCreationRequest request,
            string databaseName,
            CancellationToken cancellationToken)
        {
            var result = new TenantCreationResult();
            _logger.LogInformation("MaliDonem kaydı oluşturuluyor");
            var maliDonem = new MaliDonemModel
            {
                DatabaseType = DatabaseType.SQLite,
                FirmaId = request.FirmaId,
                MaliYil = request.MaliYil,
                DatabaseName = databaseName,
                AktifMi = true
            };
            try
            {
                await sagaStepMaliDonem.ExecuteStepAsync(
                    stepName: "MaliDonemKaydi",
                    action: async (ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        using (var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
                        {
                            try
                            {
                                await _maliDonemService.UpdateMaliDonemAsync(maliDonem,cancellationToken);
                                await _unitOfWork.SaveChangesAsync(cancellationToken);
                                await transaction.CommitAsync(cancellationToken);

                                result.MaliDonemId = maliDonem.Id;
                                _logger.LogInformation("MaliDonem kaydı oluşturuldu: {MaliDonemId}", maliDonem.Id);
                                return maliDonem.Id;
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                _logger.LogError(ex, "MaliDonem oluşturma hatası");
                                await transaction.RollbackAsync(cancellationToken);
                                throw new InvalidOperationException($"MaliDonem oluşturulamadı: {ex.Message}", ex);
                            }
                        }
                    },
                    compensate: async (maliDonemId,ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        _logger.LogWarning("Rollback: MaliDonem kaydı siliniyor: {MaliDonemId}", maliDonemId);
                        try
                        {
                            using(var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
                            {
                                var deleteMaliDonem = new MaliDonemModel { Id = maliDonemId };
                                await _maliDonemService.DeleteMaliDonemAsync(deleteMaliDonem.Id,cancellationToken);
                                await _unitOfWork.SaveChangesAsync(cancellationToken);
                                await transaction.CommitAsync(cancellationToken);
                                _logger.LogInformation("MaliDonem silindi: {MaliDonemId}", maliDonemId);
                            }
                        }

                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(ex, "MaliDonem silme hatası: {MaliDonemId}", maliDonemId);
                            // Compensate hatası fırlatmıyoruz, sadece logluyoruz
                        }
                    },cancellationToken);
                if(result.MaliDonemId <= 0)
                {
                    await _logService.SistemLogService
                        .SistemLogErrorAsync("Mali Dönem İşlemleri", "Mali Dönem Oluştur", "Mali Dönem oluşturulamadı");
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: "Mali Dönem kaydı oluşturulamadı");
                }
                await _logService.SistemLogService
                    .SistemLogInformationAsync(
                        "Mali Dönem İşlemleri",
                        "Mali Dönem Oluştur",
                        "Mali Dönem başarıyla oluşturuldu",
                        "");
                return new SuccessApiDataResponse<TenantCreationResult>(
                    data: result,
                    message: "Mali Dönem kaydı oluşturuldu");
            } catch(Exception ex)
            {
                await _logService.SistemLogService
                    .SistemLogExceptionAsync("Mali Dönem İşlemleri", "Mali Dönem Oluşturma", ex);
                return new ErrorApiDataResponse<TenantCreationResult>(
                    data: result,
                    message: $"[HATA] Mali Dönem oluşturulamadı: {ex.Message}");
            }
        }

        private async Task<ApiDataResponse<TenantCreationResult>> YeniMaliVeritabaniOlusturAsync(
            TenantOperationSaga sagaStepVeritabaniOlustur,
            TenantCreationRequest request,CancellationToken cancellationToken)
        {
            var result = new TenantCreationResult();
            _logger.LogInformation("Veritabanı dosyası oluşturuluyor");
            try
            {
                await sagaStepVeritabaniOlustur.ExecuteStepAsync(
                    stepName: "VeritabaniDosyasiOlustur",
                    action: async (ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        var dbCreateResponse = await _lifecycleService.CreateNewTenantDatabaseAsync(
                            request.DatabaseName,
                            cancellationToken);
                        if(!dbCreateResponse.Success)
                        {
                            throw new InvalidOperationException(
                                $"Veritabanı oluşturulamadı: {dbCreateResponse.Message}");
                        }
                        result.DatabaseCreated = true;
                        _logger.LogInformation("Veritabanı oluşturuldu: {Veritabanı}", result.DatabaseName);
                        return result.DatabaseName;
                    },
                    compensate: async (dbName,ct) =>
                    {
                        _logger.LogWarning("Geri Al: Veritabanı siliniyor: {Veritabanı}", dbName);
                        try
                        {
                            ct.ThrowIfCancellationRequested();
                            var deleteResponse = await _lifecycleService.DeleteTenantDatabase(
                                dbName,
                                cancellationToken);
                            if(deleteResponse.Success)
                            {
                                _logger.LogInformation("Veritabanı silindi: {Veritabanı}", dbName);
                            } else
                            {
                                _logger.LogWarning(
                                    "Veritabanı silinemedi: {Veritabanı} - {Message}",
                                    dbName,
                                    deleteResponse.Message);
                            }
                        }

                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(ex, "Veritabanı silme hatası: {Veritabanı}", dbName);
                        }
                    },cancellationToken);
                if(result.DatabaseCreated)
                {
                    await _logService.SistemLogService
                        .SistemLogErrorAsync(
                            "Mali Dönem İşlemleri",
                            "Mali Dönem Veritabanı Oluştur",
                            "Mali Dönem'e ait veritabanı oluşturulamadı");
                    return new ErrorApiDataResponse<TenantCreationResult>(
                        data: result,
                        message: "Mali Dönem'e ait veritabanı oluşturulamadı");
                }
                await _logService.SistemLogService
                    .SistemLogInformationAsync(
                        "Mali Dönem İşlemleri",
                        "Mali Dönem Oluştur",
                        "Mali Dönem'e ait veritabanı başarıyla oluşturuldu",
                        "");
                return new SuccessApiDataResponse<TenantCreationResult>(
                    data: result,
                    message: "Mali Dönem'e ait veritabanı oluşturuldu");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await _logService.SistemLogService
                    .SistemLogExceptionAsync("Mali Dönem İşlemleri", "Mali Dönem Oluşturma", ex);
                return new ErrorApiDataResponse<TenantCreationResult>(
                    data: result,
                    message: $"[HATA] Mali Dönem'e ait veritabanı oluşturulamadı: {ex.Message}");
            }
        }
        #endregion

        #region Validasyon Metodları
        private async Task<ApiDataResponse<FirmaModel>> ValidateFirmaAsync(long firmaId)
        {
            _logger.LogInformation("Firma kontrol ediliyor");
            var firmaModel = new FirmaModel { Id = firmaId };

            try
            {
                if(firmaId <= 0)
                {
                    return new ErrorApiDataResponse<FirmaModel>(
                        data: firmaModel,
                        message: "🔴 Firma ID boş veya geçersiz olamaz! ");
                }
                var firma = await _firmService.GetByFirmaIdAsync(firmaId: firmaId);
                if(!firma.Success && firma.Data == null)
                {
                    _logger.LogWarning("Firma bulunamadı: {FirmaId}", firmaId);
                    return new ErrorApiDataResponse<FirmaModel>(data: firmaModel, message: firma.Message);
                }
                _logger.LogInformation("Firma bulundu: {FirmaKodu}", firma.Data.FirmaKodu);
                firmaModel = firma.Data;
                return new SuccessApiDataResponse<FirmaModel>(data: firmaModel, message: firma.Message);
            } catch(Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Firma ve Mali Dönem oluşturma işlemlerinde hata oluştu, Veritabanı oluşturma işlemi durduruldu. FirmaId : {firmaId}",
                    firmaId);

                return new ErrorApiDataResponse<FirmaModel>(
                    data: firmaModel,
                    message: "❌ [HATA] Mali Dönem Veritanı oluşturma işlemi durduruldu");
            }
        }

        private ApiDataResponse<string> GenerateDatabaseName(string firmaKodu, int maliYil)
        {
            try
            {
                _logger.LogInformation(
                    "Veritabanı adı oluşturuluyor: Firma={FirmaKodu}, Yıl={MaliYil}",
                    firmaKodu,
                    maliYil);

                var newDbNameResponse = _lifecycleService.GenerateDatabaseName(firmaKodu, maliYil);
                
                // DOĞRU NULL CHECK:
                if(!newDbNameResponse.Success || string.IsNullOrEmpty(newDbNameResponse.Data))
                {
                    _logger.LogError("Veritabanı adı oluşturulamadı: {Message}", newDbNameResponse.Message);

                    return new ErrorApiDataResponse<string>(
                        data: string.Empty,
                        message: newDbNameResponse.Message ?? "Veritabanı adı oluşturulamadı");
                }

                _logger.LogInformation("Veritabanı adı oluşturuldu: {DatabaseName}", newDbNameResponse.Data);

                return new SuccessApiDataResponse<string>(
                    data: newDbNameResponse.Data,
                    message: newDbNameResponse.Message ?? $"✅ Veritabanı adı: {newDbNameResponse.Data}");
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Veritabanı adı oluşturma hatası");
                return new ErrorApiDataResponse<string>(
                    data: string.Empty,
                    message: $"❌ Veritabanı adı oluşturulamadı: {ex.Message}");
            }
        }

        private async Task<ApiDataResponse<bool>> ValidateMaliDonemExistsAsync(TenantCreationRequest request,CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Mali Dönem kontrol ediliyor");
                var existingDonem = await _maliDonemService.IsMaliDonemExistsAsync(request.FirmaId, request.MaliYil,cancellationToken);
                if(!existingDonem)
                {
                    _logger.LogInformation("Mali dönem mevcut değil, veritabanı oluşturma işlemi devam ediliyor");

                    return new SuccessApiDataResponse<bool>(
                        data: true,
                        message: "Mali Dönem mevcut değil, işlem devam ediyor");
                }
                _logger.LogWarning("Mali dönem zaten mevcut: {FirmaId}-{MaliYil}", request.FirmaId, request.MaliYil);

                return new ErrorApiDataResponse<bool>(
                    data: false,
                    message: $"🔴 Bu firma için {request.MaliYil} mali dönemi zaten mevcut");
            } catch(Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Mali Dönem kontrolü sırasında hata oluştu, Veritabanı oluşturma işlemi durduruldu. FirmaId : {firmaId}",
                    request.FirmaId);

                return new ErrorApiDataResponse<bool>(
                    data: false,
                    message: "❌ [HATA] Mali Dönem kontrolü sırasında bilinmeyen hata. İşlem durduruldu");
            }
        }

        private ApiDataResponse<int> IsValidMaliYil(int maliYil)
        {
            _logger.LogInformation($"Mali Yıl bilgileri kontrol ediliyor");
            if(maliYil <= 0 || maliYil < DateTime.Now.Year - 2 || maliYil > 2100)
            {
                // Ama hangi hatayı verdiğini bilmek için:
                string errorDetail = maliYil <= 0
                    ? "sıfır veya negatif"
                    : maliYil < DateTime.Now.Year - 2 ? "çok eski" : "çok ileri";
                _logger.LogError($"Geçersiz mali yil : ({errorDetail}) : {maliYil} ");
                return new ErrorApiDataResponse<int>(
                    data: 0,
                    message: $"🔴 Geçersiz mali yıl ({errorDetail}): {maliYil}");
            }
            _logger.LogInformation("Mali Yıl geçerli");
            return new SuccessApiDataResponse<int>(data: maliYil, message: "Mali yıl geçerli");
        }
        #endregion
    }
}
