using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;

namespace MuhasibPro.Data.Contracts.Database.SistemDatabase
{
    public interface ISistemDatabaseManager
    {
        Task<(bool initializeState, string message)> InitializeSistemDatabaseAsync(CancellationToken cancellationToken = default);
        Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(CancellationToken cancellationToken);
        
        Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync(CancellationToken cancellationToken);


    }
}
