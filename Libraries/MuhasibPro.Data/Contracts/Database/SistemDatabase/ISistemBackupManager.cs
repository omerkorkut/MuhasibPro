using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.SistemDatabase
{
    public interface ISistemBackupManager
    {
        Task<DatabaseBackupResult> CreateBackupAsync(DatabaseBackupType backupType);

        /// <summary>
        /// Backup'tan geri yükler
        /// </summary>
        Task<DatabaseRestoreExecutionResult> RestoreBackupAsync(string backupFileName);

        /// <summary>
        /// Mevcut backup'ları listeler
        /// </summary>
        Task<List<DatabaseBackupResult>> GetBackupsAsync();
        DateTime? GetLastBackupDate();
        Task<int> CleanOldBackupsAsync(int keepLast);
        
        Task<bool> RestoreFromLatestBackupAsync();
    }
}
