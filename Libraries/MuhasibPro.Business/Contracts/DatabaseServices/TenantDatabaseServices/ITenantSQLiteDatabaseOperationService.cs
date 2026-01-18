using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseOperationService
    {
        Task<ApiDataResponse<DatabaseBackupResult>> CreateBackupAsync(string databaseName, DatabaseBackupType backupType, CancellationToken cancellationToken=default);
        Task<ApiDataResponse<DatabaseRestoreExecutionResult>> RestoreBackupAsync(string databaseName, string backupFilePath, CancellationToken cancellationToken);
        Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync(string databaseName);
        Task<ApiDataResponse<bool>> RestoreFromLatestBackupAsync(string databaseName, CancellationToken cancellationToken);
        DateTime? GetLastBackupDate(string databaseName);
        Task<ApiDataResponse<int>> CleanOldBackupsAsync(string databaseName, int keepLast, CancellationToken cancellationToken = default);
    }
}
