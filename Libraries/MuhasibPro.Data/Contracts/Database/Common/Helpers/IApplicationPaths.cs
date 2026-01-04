namespace MuhasibPro.Data.Contracts.Database.Common.Helpers
{
    public interface IApplicationPaths
    {
        /// <summary>
        /// Sqlite -wal -shm dosylarını siler.
        /// Yedek almadan önce kullanılmalı.
        /// </summary>
        /// <param name="databaseName"></param>
        void CleanupSqliteWalFiles(string databaseName);
        bool IsSqliteDatabaseFileValid(string filePath);
        /// <summary>
        /// Kullanıcının AppData\Local\{appName} yolunu döndürür Production ortamında kullanılır
        /// </summary>
        string GetAppDataFolderPath();

        /// <summary>
        /// [ROOT]/Databases/Backup/ klasör yolunu döndürür
        /// </summary>
        string GetBackupFolderPath();

        /// <summary>
        /// [ROOT]/Databases/ klasör yolunu döndürür
        /// </summary>
        string GetDatabasesFolderPath();

        /// <summary>
        /// [ROOT]/Databases/sistem.db dosya yolunu döndürür
        /// </summary>
        string GetSistemDatabaseFilePath();

        /// <summary>
        /// Sistem veritabanının dosya boyutu döndürür
        /// </summary>

        long GetSistemDatabaseSize();

        /// <summary>
        /// [ROOT]/Databases/Backups/Tenants/ klasör yolunu döndürür
        /// </summary>
        string GetTenantBackupFolderPath();

        /// <summary>
        /// [ROOT]/Databases/Tenant/{databaseName}.db dosya yolunu döndürür Database adı güvenlik kontrollerinden geçer
        /// </summary>
        /// <exception cref="ArgumentException">Geçersiz database adı</exception>
        string GetTenantDatabaseFilePath(string databaseName);

        /// <summary>
        /// [ROOT]/Databases/Tenants/ klasör yolunu döndürür
        /// </summary>
        string GetTenantDatabaseFolderPath();

        /// <summary>
        /// Tenant veritabanının dosya boyutunu döndürür
        /// </summary>
        /// <param name="databaseName"></param>

        long GetTenantDatabaseSize(string databaseName);

        /// <summary>
        /// Sistem veritabanını dosya boyutu olarak doğrular. 
        /// </summary>

        bool IsSistemDatabaseSizeValid();
        /// <summary>
        /// Tenant veritabanını dosya boyutu olarak doğrular.
        /// </summary>
        /// <param name="databaseName"></param>

        bool IsTenantDatabaseSizeValid(string databaseName);

        /// <summary>
        /// Veritabanı adını geçersiz karakterlerden temizler
        /// </summary>
        /// <param name="databaseName"></param>

        string SanitizeDatabaseName(string databaseName);

        /// <summary>
        /// Sistem veritabanın (sistem.db) var olup olmadığını kontrol eder
        /// </summary>
        bool SistemDatabaseFileExists();

        /// <summary>
        /// Verilen veritabanının var olup olmadığını kontrol eder
        /// </summary>
        bool TenantDatabaseFileExists(string databaseName);
        bool IsSistemDatabaseValid();
        bool IsTenantDatabaseValid(string databaseName);
    }
}