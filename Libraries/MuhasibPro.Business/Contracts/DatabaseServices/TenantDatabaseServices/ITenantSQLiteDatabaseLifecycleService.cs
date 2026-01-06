using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteDatabaseLifecycleService
    {
        Task<ApiDataResponse<string>> CreateOrUpdateDatabaseAsync(string databaseName);
        Task<ApiDataResponse<bool>> DeleteDatabaseAsync(string databaseName);
        ApiDataResponse<string> GenerateDatabaseName(string firmaKodu, int maliYil);
        Task<ApiDataResponse<bool>> ValidateConnectionAsync(string databaseName);
    }
}
