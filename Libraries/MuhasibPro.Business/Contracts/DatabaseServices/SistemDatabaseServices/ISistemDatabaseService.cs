using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices
{
    public interface ISistemDatabaseService
    {
        Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync(CancellationToken cancellationToken=default);
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetHealthStatusAsync(CancellationToken cancellationToken = default);
        
        Task<(bool intializeState, string message)> InitializeSistemDatabaseAsync(CancellationToken cancellationToken=default);
        
    }
}
