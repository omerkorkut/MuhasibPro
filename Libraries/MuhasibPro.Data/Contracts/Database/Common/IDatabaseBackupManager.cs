using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.Common
{
    public interface IDatabaseBackupManager
    {
        Task<int> CleanOldBackupsAsync(string backupDir, string databaseName,
            int keepLast = 10);
        void CleanupSqliteWalFiles(string dbFilePath);
        Task ExecuteWalCheckpointAsync(string dbFilePath, string databaseName);
        Task SafeFileCopyAsync(string source, string dest);
        public bool IsValidBackupFile(string dbBackupPath, string backupFileName);
        DatabaseBackupType DetermineBackupType(string fileName);
        Task<DatabaseRestoreExecutionResult> ExecuteRestoreAsync(
            string databaseName,
            string backupSourcePath,
            string targetDbPath);
    }
}
