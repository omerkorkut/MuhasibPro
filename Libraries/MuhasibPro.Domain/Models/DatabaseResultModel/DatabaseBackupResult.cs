using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseBackupResult
    {
        public string DatabaseName { get; set; }
        public string BackupFilePath { get; set; }
        public string BackupDirectory { get; set; }
        public string BackupFileName { get; set; }
        public string BackupPath { get; set; }
        public long BackupFileSizeBytes { get; set; }
        public DatabaseBackupType BackupType { get; set; } // ⭐ ENUM!
        public bool IsBackupComleted { get; set; }
        public DateTime LastBackupDate { get; set; }
        public string Message { get; set; }

        public string BackupFileSizeDisplay => FormatFileSize(BackupFileSizeBytes);
        public string BackupDisplayName => GetBackupTypeDisplay(BackupType); // ⭐ Display property



        public string GetStatusMessage()
        {
            return IsBackupComleted
                ? "✅ Yedekleme işlemi başarıyla tamamlandı."
                : "🔴 Yedekleme başarısız: Lütfen veritabanın kullanılabilir olduğuna dikkat edin!";
        }

        private string GetBackupTypeDisplay(DatabaseBackupType type)
        {
            return type switch
            {
                DatabaseBackupType.Manual => "Manuel Yedek",
                DatabaseBackupType.Automatic => "Otomatik Yedek",
                DatabaseBackupType.Safety => "Güvenlik Yedeği",
                DatabaseBackupType.Migration => "Güncelleme Öncesi Yedek",
                DatabaseBackupType.System => "Sistem Yedeği",
                _ => "Bilinmeyen"
            };
        }
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}