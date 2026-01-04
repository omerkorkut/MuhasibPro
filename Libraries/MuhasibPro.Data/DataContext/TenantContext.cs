using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Data.DataContext
{
    /// <summary>
    /// Immutable tenant context - Minimal implementation for TenantSQLiteSelectionManager
    /// </summary>
    public sealed class TenantContext
    {
        #region Static
        public static readonly TenantContext Empty = new TenantContext();
        #endregion

        #region Properties (All Immutable)
        public string DatabaseName { get; }
        public string ConnectionString { get; }
        public DatabaseType DatabaseType { get; }
        public bool IsLoaded => !string.IsNullOrWhiteSpace(DatabaseName) &&
                               !string.IsNullOrWhiteSpace(ConnectionString);
        public bool CanConnect { get; }
        public string Message { get; }
        public DateTime LastConnectionTime { get; }
        #endregion

        #region Constructors
        private TenantContext()
        {
            DatabaseName = string.Empty;
            ConnectionString = string.Empty;
            DatabaseType = DatabaseType.SQLite;
            Message = "Veritabanı seçilmedi";
            LastConnectionTime = DateTime.MinValue;
        }

        private TenantContext(
            string databaseName,
            string connectionString,
            DatabaseType databaseType,
            bool canConnect,
            string message,
            DateTime lastConnectionTime)
        {
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            DatabaseType = databaseType;
            CanConnect = canConnect;
            Message = message ?? string.Empty;
            LastConnectionTime = lastConnectionTime;
        }
        #endregion

        #region Factory Methods (Only what's needed)
        public static TenantContext Create(
            string databaseName,
            string connectionString,
            bool canConnect = true,
            string message = null)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name required", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string required", nameof(connectionString));

            return new TenantContext(
                databaseName: databaseName,
                connectionString: connectionString,
                databaseType: DatabaseType.SQLite,
                canConnect: canConnect,
                message: message ?? "Bağlandı",
                lastConnectionTime: DateTime.UtcNow);
        }
        public static TenantContext TestConnection(
            string databaseName,
            string connectionString,
            bool canConnect = true,
            string message = null)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name required", nameof(databaseName));

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string required", nameof(connectionString));

            return new TenantContext(
                databaseName: databaseName,
                connectionString: connectionString,
                databaseType: DatabaseType.SQLite,
                canConnect: canConnect,
                message: message ?? "Bağlandı",
                lastConnectionTime: DateTime.UtcNow);
        }

        public TenantContext WithMessage(string newMessage)
        {
            if (string.Equals(Message, newMessage, StringComparison.Ordinal))
                return this;

            return new TenantContext(
                databaseName: DatabaseName,
                connectionString: ConnectionString,
                databaseType: DatabaseType,
                canConnect: CanConnect,
                message: newMessage,
                lastConnectionTime: LastConnectionTime);
        }

        public TenantContext WithRefreshedConnection()
        {
            return new TenantContext(
                databaseName: DatabaseName,
                connectionString: ConnectionString,
                databaseType: DatabaseType,
                canConnect: CanConnect,
                message: Message,
                lastConnectionTime: DateTime.UtcNow);
        }
        #endregion

        #region Basic Equality (Only what's needed)
        public override bool Equals(object obj)
        {
            return obj is TenantContext other &&
                   string.Equals(DatabaseName, other.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(ConnectionString, other.ConnectionString, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                DatabaseName?.ToUpperInvariant(),
                ConnectionString);
        }

        public static bool operator ==(TenantContext left, TenantContext right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(TenantContext left, TenantContext right)
        {
            return !(left == right);
        }
        #endregion
    }
}