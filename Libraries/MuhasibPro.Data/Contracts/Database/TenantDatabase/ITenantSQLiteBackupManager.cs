using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLiteBackupManager
    {
        Task<DatabaseBackupResult> CreateBackupAsync(string databaseName, DatabaseBackupType databaseBackup);

        /// <summary>
        /// Backup'tan geri yükler
        /// </summary>
        Task<DatabaseRestoreExecutionResult> RestoreBackupAsync(string databaseName, string backupFileName);

        /// <summary>
        /// Mevcut backup'ları listeler
        /// </summary>
        Task<List<DatabaseBackupResult>> GetBackupsAsync(string databaseName);
        DateTime? GetLastBackupDate(string databaseName);
        Task<int> CleanOldBackupsAsync(string databaseName, int keepLast = 10);
        Task<DatabaseRestoreExecutionResult> RestoreBackupDetailsAsync(string databaseName, string backupFileName);
        Task<bool> RestoreFromLatestBackupAsync(string databaseName);
        Task<DatabaseDeletingExecutionResult> DeleteBackupDatabaseAsync(
        string databaseName);
    }
}
