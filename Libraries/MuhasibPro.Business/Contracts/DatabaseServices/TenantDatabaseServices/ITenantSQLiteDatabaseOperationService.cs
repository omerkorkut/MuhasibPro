using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseOperationService
    {
        Task<ApiDataResponse<bool>> CreateBackupAsync(string databaseName);
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetHealthStatusAsync(string databaseName);
        Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync(string databaseName);
        Task<ApiDataResponse<bool>> RestoreBackupAsync(string databaseName, string backupFilePath);
        Task<ApiDataResponse<bool>> ValidateConnectionAsync(string databaseName, CancellationToken cancellationToken = default);
    }
}
