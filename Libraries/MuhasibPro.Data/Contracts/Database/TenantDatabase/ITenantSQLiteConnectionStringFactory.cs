namespace MuhasibPro.Data.Contracts.Database.TenantDatabase
{
    public interface ITenantSQLiteConnectionStringFactory
    {
        /// <summary>
        /// Database adı ve tip'e göre connection string oluşturur
        /// </summary>
        /// <param name="databaseName">Database adı (örn: FIRMA001_2024)</param>
        /// <param name="dbType">Veritabanı tipi (SQLite, SqlServer)</param>
        /// <returns>Connection string</returns>
        string CreateConnectionString(string databaseName);
        Task<(bool canConnect, string message, string connectionString)> ValidateConnectionStringAsync(string databaseName);


    }
}
