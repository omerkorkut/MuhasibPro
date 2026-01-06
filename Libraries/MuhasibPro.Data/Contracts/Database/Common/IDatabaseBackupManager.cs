using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.Common
{
    public interface IDatabaseBackupManager
    {
        Task<int> CleanOldBackupsAsync(string backupDir, string databaseName,
            int keepLast = 10, CancellationToken cancellationToken = default);
        void CleanupSqliteWalFiles(string dbFilePath);
        Task ExecuteWalCheckpointAsync(string dbFilePath, string databaseName, CancellationToken cancellationToken);
        Task SafeFileCopyAsync(string source, string dest, CancellationToken cancellationToken);
        public bool IsValidBackupFile(string dbBackupPath, string backupFileName);
        DatabaseBackupType DetermineBackupType(string fileName);
        Task<DatabaseRestoreExecutionResult> ExecuteRestoreAsync(
            string databaseName,
            string backupSourcePath,
            string targetDbPath,
            CancellationToken ct);
    }
}
