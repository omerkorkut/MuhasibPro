using MuhasibPro.Data.DataContext;

namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    /// <summary>
    /// Manages SQLite tenant database selection and switching
    /// </summary>
    public interface ITenantSQLiteSelectionManager : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether a tenant is currently loaded
        /// </summary>
        bool IsTenantLoaded { get; }

        /// <summary>
        /// Gets the current tenant context
        /// </summary>
        TenantContext GetCurrentTenant();

        /// <summary>
        /// Occurs when the current tenant changes
        /// </summary>
        event Action<TenantContext> TenantChanged;

        /// <summary>
        /// Switches to the specified tenant database
        /// </summary>
        /// <param name="databaseName">The name of the database to switch to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The new tenant context</returns>
        /// <exception cref="ArgumentException">Thrown when databaseName is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when connection to database fails</exception>
        Task<TenantContext> SwitchToTenantAsync(string databaseName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the current tenant (sets to empty)
        /// </summary>
        void ClearCurrentTenant();
    }
}