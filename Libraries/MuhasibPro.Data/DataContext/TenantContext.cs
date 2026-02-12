using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Data.DataContext
{
    /// <summary>
    /// Immutable tenant context - Minimal implementation for TenantSQLiteSelectionManager
    /// </summary>
    public class TenantContext
    {
        public string DatabaseName { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public DateTime LoadedAt { get; set; }
        public string ConnectionString { get; set; }
        public bool IsLoaded => !string.IsNullOrEmpty(DatabaseName);
        public string Message { get; set; }
        public static TenantContext Empty => new TenantContext();
    }
}