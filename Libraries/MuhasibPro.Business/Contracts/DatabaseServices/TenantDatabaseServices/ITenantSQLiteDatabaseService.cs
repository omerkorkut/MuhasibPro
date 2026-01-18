using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseService
    {
        Task<ApiDataResponse<TenantCreationResult>> CreateNewTenantDatabaseAsync(TenantCreationRequest request,CancellationToken cancellationToken);
        Task<ApiDataResponse<TenantDeletingResult>> DeleteTenantDatabaseAsync(TenantDeletingRequest request);
        Task<(bool isValid, string Message)> ValidateTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken = default);
        Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(string databaseName,CancellationToken cancellationToken );
        Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetTenantDatabaseStateAsync(string databaseName,CancellationToken cancellationToken);
        Task<ApiDataResponse<DatabaseMigrationExecutionResult>> InitializeTenantDatabaseAsync(
            string databaseName,
            CancellationToken cancellationToken);
    }
}
