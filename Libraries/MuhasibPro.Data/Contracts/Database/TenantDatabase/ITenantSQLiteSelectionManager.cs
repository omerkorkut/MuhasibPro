using MuhasibPro.Data.DataContext;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    /// <summary>
    /// Manages SQLite tenant database selection and switching
    /// </summary>
    public interface ITenantSQLiteSelectionManager
    {
        TenantContext SwitchToTenantAsync(TenantContext tenantContext);
        // Current State
        TenantContext GetCurrentTenant();
        Task<string> GetCurrentTenantConnectionStringAsync();
        bool IsTenantLoaded { get; }
        // Events (optional - for UI notifications)
        event Action<TenantContext> TenantChanged;
        void ClearCurrentTenant();



    }
}