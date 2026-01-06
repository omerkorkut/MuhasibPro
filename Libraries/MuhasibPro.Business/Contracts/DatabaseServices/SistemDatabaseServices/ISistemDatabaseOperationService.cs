using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices
{
    public interface ISistemDatabaseOperationService
    {
        Task<ApiDataResponse<bool>> ValidateConnectionAsync();
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetHealthStatusAsync();
        Task<ApiDataResponse<bool>> CreateBackupAsync();
        Task<ApiDataResponse<bool>> RestoreBackupAsync(string backupFilePath);
        Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync();
    }
}
