using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResult;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLieBackupManager
    {
        Task<DatabaseBackupResult> CreateBackupAsync(string databaseName, DatabaseBackupType databaseBackup, CancellationToken cancellationToken);

        /// <summary>
        /// Backup'tan geri yükler
        /// </summary>
        Task<bool> RestoreBackupAsync(string databaseName, string backupFileName, CancellationToken cancellationToken);

        /// <summary>
        /// Mevcut backup'ları listeler
        /// </summary>
        Task<List<DatabaseBackupResult>> GetBackupsAsync(string databaseName);
        DateTime? GetLastBackupDate(string databaseName);
        Task<int> CleanOldBackupsAsync(string databaseName, int keepLast = 10, CancellationToken cancellationToken=default);
        Task<DatabaseRestoreExecutionResult> RestoreBackupDetailsAsync(string databaseName, string backupFileName, CancellationToken cancellationToken);
        Task<bool> RestoreFromLatestBackupAsync(string databaseName, CancellationToken cancellationToken);
    }
}
