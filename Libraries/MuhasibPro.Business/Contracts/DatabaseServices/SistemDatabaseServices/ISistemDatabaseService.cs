using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices
{
    public interface ISistemDatabaseService
    {
        Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync();
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetSistemDatabaseStateAsync();
        
        Task<(bool intializeState, string message)> InitializeSistemDatabaseAsync();
        
    }
}
