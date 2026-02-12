using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Data.Database.TenantDatabase
{
    public class TenantSQLiteSelectionManager : ITenantSQLiteSelectionManager
    {

        private readonly ITenantSQLiteConnectionStringFactory _connectionStringFactory;
        private readonly ILogger<TenantSQLiteSelectionManager> _logger;
        private readonly object _tenantLock = new object();
        private TenantContext _currentTenant;

        // ⭐ Constructor düzeltildi: Kullanılmayan dependency çıkarıldı
        public TenantSQLiteSelectionManager(
            IAppDbContextFactory dbContextFactory,
            ITenantSQLiteConnectionStringFactory connectionStringFactory,
            ILogger<TenantSQLiteSelectionManager> logger) // ⭐ connectionManager ÇIKARILDI
        {
            _currentTenant = TenantContext.Empty;
            _connectionStringFactory = connectionStringFactory;
            _logger = logger;
        }

        public bool IsTenantLoaded => _currentTenant?.IsLoaded ?? false;

        public event Action<TenantContext> TenantChanged;

        public TenantContext GetCurrentTenant() => _currentTenant;

        public TenantContext SwitchToTenantAsync(TenantContext tenantContext)
        {
            lock (_tenantLock)
            {
                if (_currentTenant.DatabaseName == tenantContext.DatabaseName && _currentTenant.IsLoaded)
                {
                    _logger.LogDebug("Zaten aktif tenant: {DatabaseName}", tenantContext.DatabaseName);
                    return _currentTenant;
                }
            }
            var newTenant = tenantContext;
            

            lock (_tenantLock)
            {
                _currentTenant = newTenant;
            }
            TenantChanged?.Invoke(newTenant);
            _logger.LogInformation("Tenant değiştirildi: {DatabaseName}", tenantContext.DatabaseName);
            return _currentTenant;
        }
        // ⭐ Metod ismi ve imzası düzeltildi
        public Task<string> GetCurrentTenantConnectionStringAsync()
        {
            try
            {
                var current = GetCurrentTenant();
                return Task.FromResult(current.IsLoaded ? current.ConnectionString : string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Current tenant connection string alınamadı");
                return Task.FromResult(string.Empty);
            }
        }

        public void ClearCurrentTenant()
        {
            lock (_tenantLock)
            {
                _currentTenant = TenantContext.Empty;
            }

            _logger.LogInformation("Tenant bağlantısı temizlendi");
            TenantChanged?.Invoke(TenantContext.Empty);
        }
    }
}