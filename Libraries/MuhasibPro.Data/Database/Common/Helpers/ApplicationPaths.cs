using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using System.Text;

namespace MuhasibPro.Data.Database.Common.Helpers
{
    public static class DatabaseConstants
    {
        public const string DATABASE_FOLDER = "Databases";
        public const string SISTEM_DB_NAME = "Sistem.db";
        public const string TENANT_DATABASES_FOLDER = "Tenants";
        public const string BACKUP_FOLDER = "Backups";
        public const string TENANT_BACKUPS_FOLDER = "TenantBackups";
        public const string TEMP_FOLDER = "Temp";
        public const int MAX_DATABASE_NAME_LENGTH = 100;
        public const long MIN_SQLITE_FILE_SIZE = 100; // bytes
        public const int MAX_PARENT_DIRECTORY_LEVELS = 6;
    }

    public class ApplicationPaths : IApplicationPaths
    {
        private readonly IEnvironmentDetector _environmentDetector;
        private readonly ILogger<ApplicationPaths> _logger;
        private readonly string _applicationName;

        // Simple cache - thread-safe için Lazy<T>
        private static readonly Lazy<string> _cachedDevProjectPath = new Lazy<string>(
            () =>
            {
                var currentDir = AppContext.BaseDirectory;
                var dirInfo = new DirectoryInfo(currentDir);

                // Max 6 levels up (8 fazlaydı)
                for(int i = 0; i < 6 && dirInfo?.Parent != null; i++)
                {
                    if(dirInfo.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Length > 0 ||
                        dirInfo.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length > 0)
                        return dirInfo.FullName;

                    dirInfo = dirInfo.Parent;
                }

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MuhasibPro");
            });

        public ApplicationPaths(
            IEnvironmentDetector environmentDetector,
            ILogger<ApplicationPaths> logger,
            string applicationName = "MuhasibPro")
        {
            _environmentDetector = environmentDetector;
            _logger = logger;
            _applicationName = applicationName ?? "MuhasibPro";
        }

        #region Core Helper Methods (MERKEZİ)

        /// <summary>
        /// Güvenli dosya varlık kontrolü
        /// </summary>
        private bool SafeFileExists(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                // Path too long kontrolü (Windows için önemli)
                if(filePath.Length > 260)
                {
                    _logger.LogWarning("Dosya yolu çok uzun: {Length} karakter", filePath.Length);
                    return false;
                }
                
                return File.Exists(filePath);
            } catch(PathTooLongException ex)
            {
                _logger.LogWarning(ex, "Dosya yolu çok uzun: {Path}", filePath);
                return false;
            } catch(Exception ex) when (ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is NotSupportedException)
            {
                _logger.LogDebug(ex, "Dosya kontrolü başarısız: {Path}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Güvenli dizin oluşturma
        /// </summary>
        public string SafeCreateDirectory(string directoryPath)
        {
            if(string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Dizin yolu boş olamaz");

            try
            {
                Directory.CreateDirectory(directoryPath);
                return directoryPath;
            } catch(Exception ex) when (ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is NotSupportedException)
            {
                _logger.LogError(ex, "Dizin oluşturma başarısız: {Path}", directoryPath);
                throw new InvalidOperationException($"Dizin oluşturulamadı: {directoryPath}", ex);
            }
        }
            #endregion

        #region Base Paths
        public string GetAppDataFolderPath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _applicationName);

            return SafeCreateDirectory(path);
        }

        private string GetDevelopmentProjectFolderPath() => _cachedDevProjectPath.Value;

        private string GetRootDataPath()
        { return _environmentDetector.IsDevelopment() ? GetDevelopmentProjectFolderPath() : GetAppDataFolderPath(); }
            #endregion

        #region Databases Structure
        // [ROOT]/Databases/
        public string GetDatabasesFolderPath()
        {
            var path = Path.Combine(GetRootDataPath(), DatabaseConstants.DATABASE_FOLDER);
            return SafeCreateDirectory(path);
        }

        // [ROOT]/Databases/Tenant/
        public string GetTenantDatabaseFolderPath()
        {
            var path = Path.Combine(GetDatabasesFolderPath(), DatabaseConstants.TENANT_DATABASES_FOLDER);
            return SafeCreateDirectory(path);
        }

        // [ROOT]/Databases/sistem.db
        public string GetSistemDatabaseFilePath()
        { return Path.Combine(GetDatabasesFolderPath(), DatabaseConstants.SISTEM_DB_NAME); }

        // [ROOT]/Databases/Tenant/{databaseName}.db
        public string GetTenantDatabaseFilePath(string databaseName)
        {
            var sanitizedName = SanitizeDatabaseName(databaseName);
            var tenantPath = GetTenantDatabaseFolderPath();

            if(!sanitizedName.AsSpan().EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(tenantPath, sanitizedName + ".db");
            return Path.Combine(tenantPath, sanitizedName);
        }

        // Basit ve güvenli sanitize
        private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        public string SanitizeDatabaseName(string databaseName)
        {
            if(string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database adı boş olamaz");

            // Trim et ve kontrol et
            var trimmed = databaseName.Trim();

            if(trimmed.Length > DatabaseConstants.MAX_DATABASE_NAME_LENGTH)
                throw new ArgumentException(
                    $"Database adı {DatabaseConstants.MAX_DATABASE_NAME_LENGTH} karakterden uzun olamaz");


            // LINQ yerine StringBuilder (daha verimli)
            var sanitized = new StringBuilder(trimmed.Length);
            foreach(char c in trimmed)
            {
                if(Array.IndexOf(_invalidFileNameChars, c) == -1) // Contains yerine
                    sanitized.Append(c);
            }

            var result = sanitized.ToString().Trim();

            if(string.IsNullOrEmpty(result))
                throw new ArgumentException("Database adı geçersiz");

            // Windows rezerve dosya isimleri
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(result);

            if(_reservedNames.Contains(fileNameWithoutExt))
                throw new ArgumentException($"'{result}' rezerve bir dosya adıdır");

            // Sonunda .db yoksa ekle
            if(!result.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                result += ".db";

            return result;
        }
        #endregion

        #region Backup Structure
        // [ROOT]/Databases/Backups/
        public string GetBackupFolderPath()
        {
            var path = Path.Combine(GetDatabasesFolderPath(), DatabaseConstants.BACKUP_FOLDER);
            return SafeCreateDirectory(path);
        }

        // [ROOT]/Databases/Backups/Tenant/
        public string GetTenantBackupFolderPath()
        {
            var path = Path.Combine(GetBackupFolderPath(), DatabaseConstants.TENANT_BACKUPS_FOLDER);
            return SafeCreateDirectory(path);
        }
            #endregion

        #region Database Exists & Validation
        public bool SistemDatabaseFileExists()
        {
            var filePath = GetSistemDatabaseFilePath();
            return SafeFileExists(filePath);           
           
        }

        public bool TenantDatabaseFileExists(string databaseName)
        {
            var filePath = GetTenantDatabaseFilePath(databaseName);
            return SafeFileExists(filePath);            
        }

        public long GetSistemDatabaseSize()
        {
            var dbFilePath = GetSistemDatabaseFilePath();
            return GetFileSizeSafe(dbFilePath);
        }

        public long GetTenantDatabaseSize(string databaseName)
        {
            var dbFilePath = GetTenantDatabaseFilePath(databaseName);
            return GetFileSizeSafe(dbFilePath);
        }

        private long GetFileSizeSafe(string filePath)
        {
            if(!SafeFileExists(filePath))
                return 0L;

            try
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            } catch(Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _logger.LogDebug(ex, "Dosya boyutu alınamadı: {Path}", filePath);
                return 0L;
            }
        }

        public bool IsSistemDatabaseSizeValid()
        {
            var dbPath = GetSistemDatabaseFilePath();
            return IsDatabaseSizeValid(dbPath);
        }

        public bool IsTenantDatabaseSizeValid(string databaseName)
        {
            var dbPath = GetTenantDatabaseFilePath(databaseName);
            return IsDatabaseSizeValid(dbPath);
        }

        private bool IsDatabaseSizeValid(string filePath)
        {
            var size = GetFileSizeSafe(filePath);
            return size >= DatabaseConstants.MIN_SQLITE_FILE_SIZE;
        }
        #endregion

        #region SQLite Validation
        public bool IsSqliteDatabaseFileValid(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return false;

            if(!SafeFileExists(filePath))
                return false;

            var fileInfo = GetFileInfoSafe(filePath);
            if(fileInfo == null || fileInfo.Length < DatabaseConstants.MIN_SQLITE_FILE_SIZE)
                return false;

            return ValidateSqliteHeader(filePath);
        }

        public bool IsSistemDatabaseValid()
        {
            var filePath = GetSistemDatabaseFilePath();
            return IsSqliteDatabaseFileValid(filePath);
        }

        public bool IsTenantDatabaseValid(string databaseName)
        {
            var filePath = GetTenantDatabaseFilePath(databaseName);
            return IsSqliteDatabaseFileValid(filePath);
        }

        private FileInfo GetFileInfoSafe(string filePath)
        {
            try
            {
                return new FileInfo(filePath);
            } catch(Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _logger.LogDebug(ex, "FileInfo oluşturulamadı: {Path}", filePath);
                return null;
            }
        }

        private static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

        private static readonly byte[] _sqliteMagicBytes = new byte[]
        {
            0x53,
            0x51,
            0x4C,
            0x69,
            0x74,
            0x65,
            0x20,
            0x66,
            0x6F,
            0x72,
            0x6D,
            0x61,
            0x74,
            0x20,
            0x33,
            0x00
        };

        private bool ValidateSqliteHeader(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Sadece ilk 16 byte oku (daha verimli)
                Span<byte> header = stackalloc byte[16];
                int bytesRead = fs.Read(header);

                if(bytesRead < 16)
                    return false;

                // MemoryExtensions.SequenceEqual kullan (daha hızlı)
                return header.SequenceEqual(_sqliteMagicBytes.AsSpan());
            } catch(Exception ex) when (ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is NotSupportedException)
            {
                _logger.LogDebug(ex, "SQLite header kontrolü başarısız: {Path}", filePath);
                return false;
            }
        }
        #endregion

        #region Helper Methods
        public void CleanupSqliteWalFiles(string databaseName)
        {
            try
            {
                var dbPath = GetTenantDatabaseFilePath(databaseName);
                var directory = Path.GetDirectoryName(dbPath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(dbPath);

                // Tüm SQLite journal dosyalarını temizle
                var journalFiles = new[]
                {
                    $"{fileNameWithoutExt}-wal",
                    $"{fileNameWithoutExt}-shm",                    
                };

                foreach(var journalFile in journalFiles)
                {
                    var journalPath = Path.Combine(directory, journalFile);
                    SafeDeleteFile(journalPath);
                }
            } catch(Exception ex)
            {
                // Debug yerine Warning (daha uygun)
                _logger.LogWarning(ex, "SQLite journal dosyaları temizlenemedi: {Db}", databaseName);
            }
        }

        private void SafeDeleteFile(string filePath)
        {
            try
            {
                if(File.Exists(filePath))
                    File.Delete(filePath);
            } catch(Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _logger.LogDebug(ex, "Dosya silinemedi: {Path}", filePath);
            }
        }
        #endregion
    }
}
