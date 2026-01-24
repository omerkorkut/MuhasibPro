using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common
{
    public interface ITenantBackupService
    {
        Task<ApiDataResponse<TenantDeletingResult>> BackupCurrentTenantIfNeededAsync(
        TenantDeletingRequest request);
        Task<ApiDataResponse<TenantDeletingResult>> CleanAllBackupsAsync(string databaseName);
        Task<bool> CleanupBackupFileAsync(string backupFilePath);
    }
}
