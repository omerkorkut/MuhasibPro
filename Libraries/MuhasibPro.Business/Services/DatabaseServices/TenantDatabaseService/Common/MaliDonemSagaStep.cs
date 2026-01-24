using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService.Common
{
    public class MaliDonemSagaStep : IMaliDonemSagaStep
    {
        private readonly IMaliDonemService _maliDonemService;
        private readonly IUnitOfWork<SistemDbContext> _unitOfWork;

        public MaliDonemSagaStep(IMaliDonemService maliDonemService, IUnitOfWork<SistemDbContext> unitOfWork)
        {
            _maliDonemService = maliDonemService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiDataResponse<TenantCreationResult>> CreateNewMaliDonemAsync(
            TenantOperationSaga sagaStepNewMaliDonem,
            TenantCreationRequest request)
        {
            // ✅ DOĞRU: MaliDonemId = 0 başlangıçta
            var result = new TenantCreationResult
            {
                DatabaseName = request.DatabaseName, // Ek bilgi
                FirmaId = request.FirmaId,
                MaliYil = request.MaliYil
            };

            var maliDonem = new MaliDonemModel
            {
                DatabaseType = Domain.Enum.DatabaseEnum.DatabaseType.SQLite,
                DatabaseName = request.DatabaseName,
                FirmaId = request.FirmaId,
                MaliYil = request.MaliYil,
                AktifMi = true
            };

            try
            {
                await sagaStepNewMaliDonem.ExecuteStepAsync(
                    stepName: "NewMaliDonem",
                    action: async () =>
                    {
                        using var transaction = await _unitOfWork.BeginTransactionAsync();

                        await _maliDonemService.UpdateMaliDonemAsync(maliDonem);
                        await _unitOfWork.SaveChangesAsync();
                        await transaction.CommitAsync();

                        result.MaliDonemId = maliDonem.Id; // ⭐ Burada set ediliyor
                        return maliDonem.Id;
                    },
                    compensate: async (maliDonemId) =>
                    {
                        try
                        {
                            if (maliDonemId > 0)
                            {
                                using var transaction = await _unitOfWork.BeginTransactionAsync();
                                await _maliDonemService.DeleteMaliDonemAsync(maliDonemId);
                                await _unitOfWork.SaveChangesAsync();
                                await transaction.CommitAsync();
                            }
                        }
                        catch { /* Ignore compensation errors */ }
                    });

                // ✅ result.MaliDonemId kontrolü
                if (result.MaliDonemId <= 0)
                    return ApiDataExtensions.ErrorResponse(result, "Mali Dönem Kaydı Oluşturulamadı");

                return ApiDataExtensions.SuccessResponse(result,
                    $"Mali Dönem Kaydı Başarıyla Oluşturuldu (ID: {result.MaliDonemId})");
            }
            catch (Exception ex)
            {
                // ✅ SADECE return yap, throw YAPMA!
                return ApiDataExtensions.ErrorResponse(result,
                    $"[HATA] Mali Dönem kaydı oluşturulamadı: {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<TenantDeletingResult>> DeleteMaliDonemAsync(
            TenantOperationSaga sagaStepDeleteMaliDonem,
            TenantDeletingRequest request)
        {
            var result = new TenantDeletingResult
            {
                MaliDonemId = request.MaliDonemId,
                MaliDonemDeleted = false
            };            
            if(request.MaliDonemId <= 0)
            {
                return ApiDataExtensions.ErrorResponse(result, "Geçersiz Mali Dönem Id");
            }
            var maliDonemResponse = await _maliDonemService.GetByMaliDonemIdAsync(request.MaliDonemId);
            if(!maliDonemResponse.Success || maliDonemResponse.Data == null)
            {
                return ApiDataExtensions.ErrorResponse(result, "Mali Dönem Bulunamadı");
            }
            try
            {
                var maliDonemToRestore = maliDonemResponse.Data;
                await sagaStepDeleteMaliDonem.ExecuteStepAsync(
                    stepName: "DeleteMaliDonem",
                    action: async () =>
                    {
                        using var transaction = await _unitOfWork.BeginTransactionAsync();

                        await _maliDonemService.DeleteMaliDonemAsync(request.MaliDonemId);
                        await _unitOfWork.SaveChangesAsync();
                        await transaction.CommitAsync();

                        result.MaliDonemDeleted = true;
                        return request.MaliDonemId;
                    },
                    compensate: async (deletedId) =>
                    {
                        try
                        {
                            if (maliDonemToRestore != null)
                            {
                                using var transaction = await _unitOfWork.BeginTransactionAsync();
                                await _maliDonemService.RestoreMaliDonemAsync(maliDonemToRestore);
                                await _unitOfWork.SaveChangesAsync();
                                await transaction.CommitAsync();
                            }
                        }
                        catch { /* Ignore compensation errors */ }
                    });

                // ✅ result.MaliDonemId kontrolü
                if (!result.MaliDonemDeleted)
                    return ApiDataExtensions.ErrorResponse(result, "Mali Dönem Kaydı Silinemedi");

                return ApiDataExtensions.SuccessResponse(result,
                    $"Mali Dönem Kaydı Başarıyla Silindi (ID: {result.MaliDonemId})");
            }
            catch (Exception ex)
            {
                // ✅ SADECE return yap, throw YAPMA!
                return ApiDataExtensions.ErrorResponse(result,
                    $"[HATA] Mali Dönem kaydı silinemedi: {ex.Message}");
            }
        }

    }
}
 