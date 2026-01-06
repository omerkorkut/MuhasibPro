using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteSelectionService
    {
        Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(string databaseName);
        Task<ApiDataResponse<bool>> DisconnectCurrentTenantAsync();
        ApiDataResponse<TenantContext> GetCurrentTenant();
        bool IsConnected { get; }
        void ClearCurrentTenant();
    }
}