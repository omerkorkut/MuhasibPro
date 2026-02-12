using Microsoft.Data.Sqlite;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService.Common
{
    public class TenantDatabaseSagaStep: ITenantDatabaseSagaStep
    {
        private readonly ITenantSQLiteDatabaseLifecycleService _lifecycleService;
        private readonly ITenantBackupService _backupService;
        private readonly ITenantSQLiteDatabaseOperationService _operationService;
        private readonly ITenantSQLiteSelectionService _selectionService;
        private readonly IApplicationPaths _applicationPaths;
        

        public TenantDatabaseSagaStep(
            ITenantSQLiteDatabaseLifecycleService lifecycleService,
            ITenantBackupService backupService,
            IApplicationPaths applicationPaths,
            ITenantSQLiteDatabaseOperationService operationService,
            ITenantSQLiteSelectionService selectionService,
            ITenantSQLiteBackupManager tenantSQLieBackupManager)
        {
            _lifecycleService = lifecycleService;
            _backupService = backupService;
            _applicationPaths = applicationPaths;
            _operationService = operationService;
            _selectionService = selectionService;
            
        }

        public async Task<ApiDataResponse<TenantCreationResult>> CreateTenantDatabaseAsync(
            TenantOperationSaga sagaStepCreateDatabase,
            string databaseName)
        {
            var result = new TenantCreationResult { DatabaseName = databaseName, DatabaseCreated = false, };

            if(string.IsNullOrWhiteSpace(databaseName))
                return ApiDataExtensions.ErrorResponse(result, "DatabaseName boş olamaz");

            try
            {
                await sagaStepCreateDatabase.ExecuteStepAsync(
                    stepName: "VeritabaniDosyasiOlustur",
                    action: async () =>
                    {
                        var createResponse = await _lifecycleService.CreateNewTenantDatabaseAsync(databaseName);

                        if(!createResponse.Success)
                        {
                            throw new InvalidOperationException($"Veritabanı oluşturulamadı: {createResponse.Message}");
                        }

                        result.DatabaseCreated = true;
                        return databaseName;
                    },
                    compensate: async (dbName) =>
                    {
                        if(string.IsNullOrWhiteSpace(dbName))
                            return;

                        try
                        {
                            await _lifecycleService.DeleteTenantDatabase(dbName);
                        } catch(Exception)
                        {
                        }
                    });

                // Saga başarılı çalıştıysa buraya gelir
                if(!result.DatabaseCreated)
                {
                    return ApiDataExtensions.ErrorResponse(result, "Veritabanı oluşturuldu ama status güncellenemedi");
                }

                return ApiDataExtensions.SuccessResponse(result, $"{databaseName} veritabanı oluşturuldu");
            } catch(Exception ex)
            {
                return ApiDataExtensions.ErrorResponse(result, $"[HATA] Veritabanı oluşturulamadı: {ex.Message}");
            }
        }

        private async Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabase(TenantDeletingRequest request)
        {
            var result = new TenantDeletingResult
            {
                DatabaseName = request.DatabaseName,
                DatabaseDeleted = false,
                IsCurrentTenantDeletingBeforeBackup = request.IsCurrentTenantDeletingBeforeBackup,
                BackupDeleteCompleted = false,
                BackupCreateCompleted = false,
                DeletedBackupCount = 0,
                DeletedBackupFiles = new List<string>(),
            };
            if(string.IsNullOrWhiteSpace(request.DatabaseName))
                return ApiDataExtensions.ErrorResponse(result, "Veritabanı adı boş olamaz");
            try
            {
                if(request.DeleteAllTenantBackup)
                {
                    var allDeletingBackups = await _backupService.CleanAllBackupsAsync(request.DatabaseName);
                    if(!allDeletingBackups.Success && !allDeletingBackups.Data.BackupDeleteCompleted)
                    {
                        result.DatabaseDeleted = false;
                        result.DeletedBackupFiles = allDeletingBackups.Data.DeletedBackupFiles;
                        result.DeletedBackupCount = allDeletingBackups.Data.DeletedBackupCount;                        
                        result.BackupDeleteCompleted = allDeletingBackups.Data.BackupDeleteCompleted;
                        return ApiDataExtensions.ErrorResponse(result, $"Veritabanı yedekleri silinemedi");
                    }
                } else
                {
                    result.BackupDeleteCompleted = false;
                    result.SuccessMessage = "Veritabanına ait yedeklerin silinmesi kullanıcı tarafından istenmedi \n Diğer işlemler devam ediyor";
                }


                if(request.IsDeleteDatabase)
                {
                    var deletingTenant = await _lifecycleService.DeleteTenantDatabase(request.DatabaseName);
                    if(!deletingTenant.Success || !deletingTenant.Data.IsDeletedSuccess)
                    {
                        if(!string.IsNullOrEmpty(result.BackupFilePath))
                        {
                            await _backupService.CleanupBackupFileAsync(result.BackupFilePath);
                        }
                        result.BackupCreateCompleted = false;
                        result.DatabaseDeleted = false;
                        result.IsCurrentTenantDeletingBeforeBackup = false;
                        return ApiDataExtensions.ErrorResponse(result, deletingTenant.Message);
                    }
                    result.DatabaseDeleted = true;
                    result.DeleteCompleted = true;
                }
                return ApiDataExtensions.SuccessResponse(
                    result,
                    $"Mali Dönem'e ait {result.DatabaseName} başarıyla silindi");
            } catch(Exception ex)
            {
                return ApiDataExtensions.ErrorResponse(
                    result,
                    $"Mali Dönem'e ait {result.DatabaseName} veritabanı silinemedi {ex.Message}");
            }
        }

        public async Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabaseAsync(
            TenantOperationSaga sagaStepDeleteDatabase,
            TenantDeletingRequest request)
        {
            var result = new TenantDeletingResult
            {
                DatabaseName = request.DatabaseName,
                DatabaseDeleted = false,
                IsCurrentTenantDeletingBeforeBackup = false,
                
            };
            var safetyBackupFilePath = string.Empty;
            try
            {
                bool isCurrentTenantDeleting = false;
                if (_selectionService.IsTenantLoaded)
                {
                    var currentTenantResponse = _selectionService.CurrentTenant;
                    if (currentTenantResponse.IsLoaded)
                    {
                        isCurrentTenantDeleting = currentTenantResponse.DatabaseName == request.DatabaseName;
                    }
                }
                if (request.IsDeleteDatabase)
                {
                    await sagaStepDeleteDatabase.ExecuteStepAsync(
                        stepName: "DeleteDatabase",
                        action: async () =>
                        {
                            var sourceDbPath = _applicationPaths.GetTenantDatabaseFilePath(request.DatabaseName);
                            if (File.Exists(sourceDbPath))
                            {
                                var backupPath = _applicationPaths.GetTenantBackupFolderPath();
                                var backupFileName = $"safety_{request.DatabaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                                var backupFilePath = Path.Combine(backupPath, backupFileName);

                                SqliteConnection.ClearAllPools();
                                await Task.Delay(50);

                                await SafeFileCopyAsync(sourceDbPath, backupFilePath);
                                result.BackupFilePath = backupFilePath;
                                safetyBackupFilePath = backupFilePath;
                            }
                            if (request.IsCurrentTenantDeletingBeforeBackup || isCurrentTenantDeleting)
                            {
                                result.IsCurrentTenantDeletingBeforeBackup = true;
                                result.BackupCreateCompleted = true;
                            }
                            else
                            {
                                result.IsCurrentTenantDeletingBeforeBackup = false;
                                result.BackupCreateCompleted = false;
                            }
                            var dbDeleteResponse = await DeleteTenantDatabase(request);
                            if (!dbDeleteResponse.Success || dbDeleteResponse.Data == null)
                            {
                                throw new InvalidOperationException($"Veritabanı silinemedi: {dbDeleteResponse.Message}");
                            }                            
                            result = dbDeleteResponse.Data;
                            return request.DatabaseName;
                        },
                        compensate: async (databaseName) =>
                        {
                            if (!string.IsNullOrEmpty(result.BackupFilePath) && File.Exists(result.BackupFilePath))
                            {
                                try
                                {
                                    var restoreResponse = await _operationService.RestoreBackupAsync(
                                        request.DatabaseName,
                                        result.BackupFilePath);

                                }
                                catch (Exception)
                                {
                                }
                            }
                        });
                    if (isCurrentTenantDeleting)
                    {
                        _selectionService.ClearCurrentTenantAsync();
                    }
                    if (!request.IsCurrentTenantDeletingBeforeBackup)
                {
                        await sagaStepDeleteDatabase.ExecuteStepAsync(
                            stepName: "CleanBackup",
                            action: async () =>
                            {
                                await _backupService.CleanupBackupFileAsync(safetyBackupFilePath);
                                return safetyBackupFilePath;
                            },
                            compensate: null);
                    }

                }
                return ApiDataExtensions.SuccessResponse(data: result, "Veritabanı başarıyla silindi");
            }
            catch (Exception ex)
            {
                await sagaStepDeleteDatabase.CompensateAllAsync();
                return ApiDataExtensions.ErrorResponse(result, $"Veritabanı silme hatası :{ex.Message}");
            }
        }

        private async Task SafeFileCopyAsync(string source, string dest)
        {
            SqliteConnection.ClearAllPools();
            await Task.Delay(100);

            using(var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using(var destStream = new FileStream(dest, FileMode.Create))
                {
                    await sourceStream.CopyToAsync(destStream);
                }
        }
    }
}
