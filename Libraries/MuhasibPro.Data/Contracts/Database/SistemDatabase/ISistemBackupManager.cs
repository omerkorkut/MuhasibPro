using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.SistemDatabase
{
    public interface ISistemBackupManager
    {
        Task<DatabaseBackupResult> CreateBackupAsync(DatabaseBackupType backupType, CancellationToken cancellationToken);

        /// <summary>
        /// Backup'tan geri yükler
        /// </summary>
        Task<DatabaseRestoreExecutionResult> RestoreBackupAsync(string backupFileName, CancellationToken cancellationToken);

        /// <summary>
        /// Mevcut backup'ları listeler
        /// </summary>
        Task<List<DatabaseBackupResult>> GetBackupsAsync();
        DateTime? GetLastBackupDate();
        Task<int> CleanOldBackupsAsync(int keepLast, CancellationToken cancellationToken = default);
        
        Task<bool> RestoreFromLatestBackupAsync(CancellationToken cancellationToken);
    }
}
