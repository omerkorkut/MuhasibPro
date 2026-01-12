using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.DataContext;
using System.Collections.Concurrent;

namespace MuhasibPro.Data.Database.TenantDatabase
{
    public sealed class TenantSQLiteSelectionManager : ITenantSQLiteSelectionManager, IAsyncDisposable
    {
        private readonly ITenantSQLiteConnectionStringFactory _connectionStringFactory;
        private readonly ILogger<TenantSQLiteSelectionManager> _logger;

        private readonly SemaphoreSlim _tenantSemaphore = new(1, 1);
        private readonly ConcurrentDictionary<string, ConnectionCacheEntry> _connectionCache = new();

        private TenantContext _currentTenant = TenantContext.Empty;

        private event Action<TenantContext> TenantChangedInternal;
        private readonly object _eventLock = new();
        private bool _disposed;

        public TenantSQLiteSelectionManager(
            ITenantSQLiteConnectionStringFactory connectionStringFactory,
            ILogger<TenantSQLiteSelectionManager> logger)
        {
            _connectionStringFactory = connectionStringFactory;
            _logger = logger;
        }

        #region Public Interface
        public bool IsTenantLoaded => _currentTenant.IsLoaded;

        public event Action<TenantContext> TenantChanged
        {
            add
            {
                if(value == null)
                    return;
                lock(_eventLock)
                    TenantChangedInternal += value;
            }
            remove
            {
                if(value == null)
                    return;
                lock(_eventLock)
                    TenantChangedInternal -= value;
            }
        }

        public TenantContext GetCurrentTenant() => _currentTenant;

        public async Task<TenantContext> SwitchToTenantAsync(
            string databaseName,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name required", nameof(databaseName));

            ThrowIfDisposed();

            await _tenantSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await SwitchToTenantCoreAsync(databaseName, cancellationToken);
            } finally
            {
                _tenantSemaphore.Release();
            }
        }

        public async Task ClearCurrentTenantAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _tenantSemaphore.WaitAsync(cancellationToken);
            try
            {
                var oldTenant = _currentTenant;
                _currentTenant = TenantContext.Empty;

                SafeInvokeTenantChanged(TenantContext.Empty);
                _logger.LogInformation("Tenant cleared: {DatabaseName}", oldTenant.DatabaseName);
            } finally
            {
                _tenantSemaphore.Release();
            }
        }

        
        #endregion

        #region Core Logic
        private async Task<TenantContext> SwitchToTenantCoreAsync(
            string databaseName,
            CancellationToken cancellationToken)
        {
            // 1. Check if already current
            if(_currentTenant.DatabaseName == databaseName && _currentTenant.IsLoaded)
            {
                var updated = _currentTenant.WithRefreshedConnection();
                _currentTenant = updated;
                return updated;
            }

            // 2. Test connection
            var connectionResult = await _connectionStringFactory.ValidateConnectionStringAsync(
                databaseName,
                cancellationToken);

            if(!connectionResult.canConnect)
            {
                var errorTenant = _currentTenant.WithMessage($"Bağlantı hatası: {connectionResult.message}");
                _currentTenant = errorTenant;
                return errorTenant;
            }

            // 3. Create new tenant
            var newTenant = TenantContext.Create(
                databaseName: databaseName,
                connectionString: connectionResult.connectionString,
                canConnect: true,
                message: connectionResult.message);

            // 4. Switch
            var oldTenant = _currentTenant;
            _currentTenant = newTenant;

            // 5. Notify
            SafeInvokeTenantChanged(newTenant);
            _logger.LogInformation("Tenant switched: {Old} -> {New}", oldTenant.DatabaseName, newTenant.DatabaseName);

            return newTenant;
        }

        private void SafeInvokeTenantChanged(TenantContext tenant)
        {
            Action<TenantContext> handlers;
            lock(_eventLock)
            {
                handlers = TenantChangedInternal;
            }

            if(handlers == null)
                return;

            foreach(Action<TenantContext> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(tenant);
                } catch(Exception ex)
                {
                    _logger.LogError(ex, "TenantChanged handler error");
                }
            }
        }
        #endregion


        #region Disposal
        private void ThrowIfDisposed()
        {
            if(_disposed)
                throw new ObjectDisposedException(nameof(TenantSQLiteSelectionManager));
        }

        public async ValueTask DisposeAsync()
        {
            if(_disposed)
                return;

            await ClearCurrentTenantAsync();
            _tenantSemaphore.Dispose();
            _connectionCache.Clear();
            _disposed = true;
        }

        public void Dispose() { DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        #endregion

        #region Helper Types
        private record ConnectionCacheEntry(string ConnectionString, DateTime LastValidated, string DatabasePath);
        #endregion
    }
}