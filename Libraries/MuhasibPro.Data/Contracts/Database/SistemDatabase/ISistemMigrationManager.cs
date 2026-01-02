using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Concrete.Database.SistemDatabase
{
    public interface ISistemMigrationManager
    {
        Task<bool> InitializeSistemDatabaseAsync(CancellationToken cancellationToken = default);
        Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(CancellationToken cancellationToken = default);
        Task<List<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
        Task<string> GetCurrentDatabaseVersionAsync(CancellationToken cancellationToken = default);


    }
}
