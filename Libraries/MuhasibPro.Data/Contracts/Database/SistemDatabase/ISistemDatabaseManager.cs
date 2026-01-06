using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Contracts.Database.SistemDatabase
{
    public interface ISistemDatabaseManager
    {
        Task<bool> InitializeSistemDatabaseAsync(CancellationToken cancellationToken = default);
        Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(CancellationToken cancellationToken);
        Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync(CancellationToken cancellationToken);


    }
}
