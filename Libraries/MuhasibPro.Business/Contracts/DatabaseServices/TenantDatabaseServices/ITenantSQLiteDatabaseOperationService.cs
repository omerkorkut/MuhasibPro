using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseOperationService
    {
        Task<ApiDataResponse<DatabaseBackupResult>> CreateBackupAsync(string databaseName, DatabaseBackupType backupType);
        Task<ApiDataResponse<DatabaseRestoreExecutionResult>> RestoreBackupAsync(string databaseName, string backupFilePath);
        Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync(string databaseName);
        Task<ApiDataResponse<bool>> RestoreFromLatestBackupAsync(string databaseName);
        DateTime? GetLastBackupDate(string databaseName);
        Task<ApiDataResponse<int>> CleanOldBackupsAsync(string databaseName, int keepLast);
        Task<DatabaseDeletingExecutionResult> DeleteBackupDatabaseAsync(
        string databaseName);
    }
}
