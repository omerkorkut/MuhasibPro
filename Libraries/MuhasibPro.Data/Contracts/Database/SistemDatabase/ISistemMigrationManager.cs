using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;

namespace MuhasibPro.Data.Contracts.Database.SistemDatabase
{
    public interface ISistemMigrationManager
    {
        Task<(bool initializeState, string message)> InitializeSistemDatabaseAsync(CancellationToken cancellationToken);

        Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(CancellationToken cancellationToken = default);

        Task<DatabaseHealtyDiagReport> GetSistemDatabaseFullDiagStateAsync(
            IProgress<AnalysisProgress> progressReporter = null,
            AnalysisOptions options = null,
            CancellationToken cancellationToken = default);

        Task<List<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

        Task<string> GetCurrentDatabaseVersionAsync(CancellationToken cancellationToken = default);
    }
}
