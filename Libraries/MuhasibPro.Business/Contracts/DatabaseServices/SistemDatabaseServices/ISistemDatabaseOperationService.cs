using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices
{
    public interface ISistemDatabaseOperationService
    {
        Task<ApiDataResponse<DatabaseBackupResult>> CreateBackupAsync(DatabaseBackupType backupType, CancellationToken cancellationToken);
        Task<ApiDataResponse<DatabaseRestoreExecutionResult>> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken);
        Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync();
        Task<ApiDataResponse<bool>> RestoreFromLatestBackupAsync(CancellationToken cancellationToken);
        DateTime? GetLastBackupDate();
        Task<ApiDataResponse<int>> CleanOldBackupsAsync(int keepLast, CancellationToken cancellationToken = default);
    }
}
