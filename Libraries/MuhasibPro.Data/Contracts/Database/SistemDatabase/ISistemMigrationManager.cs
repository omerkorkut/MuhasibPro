using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;

namespace MuhasibPro.Data.Contracts.Database.SistemDatabase
{
    public interface ISistemMigrationManager
    {
        Task<(bool initializeState, string message)> InitializeSistemDatabaseAsync();

        Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync();

        Task<DatabaseHealtyDiagReport> GetSistemDatabaseFullDiagStateAsync(
            IProgress<AnalysisProgress> progressReporter = null,
            AnalysisOptions options = null);

        Task<List<string>> GetPendingMigrationsAsync();

        Task<string> GetCurrentDatabaseVersionAsync();
    }
}
