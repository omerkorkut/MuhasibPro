using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseConnectionAnalysis : BaseDatabaseResult
    {
        // Analiz Özgü Property'ler
        public bool IsDatabaseExists { get; set; }
        public bool DatabaseValid { get; set; }
        public string CurrentVersion { get; set; }

        public List<string> PendingMigrations { get; set; } = new();
        public int AppliedMigrationsCount { get; set; }
        public override bool IsUpdateRequired => PendingMigrations.Any();
        public int TableCount { get; set; }
        public bool IsEmptyDatabase { get; set; }
        public long DatabaseFileSizeBytes { get; set; }
        public string FileSizeDisplay => FormatFileSize(DatabaseFileSizeBytes);
        public bool ShouldTakeBackupBeforeMigration => IsUpdateRequired && !IsEmptyDatabase;
        // Etiket ismi
        public override string OperationDisplayName => "Analiz";

        // Akıllı Durum Belirleme
        public override DatabaseStatusResult GetStatus()
        {
            if (!IsDatabaseExists) return DatabaseStatusResult.DatabaseNotFound;
            if (HasError) return DatabaseStatusResult.UnknownError;
            if (!CanConnect) return DatabaseStatusResult.ConnectionFailed;
            if (!DatabaseValid) return DatabaseStatusResult.InvalidSchema;

            // Eğer her şey teknik olarak tamamsa ama bekleyen güncelleme varsa
            if (IsUpdateRequired) return DatabaseStatusResult.RequiredUpdating;

            return DatabaseStatusResult.Success;
        }

        // Akıllı Mesaj Yönetimi
        public override string GetStatusMessage()
        {
            var status = GetStatus();

            return status switch
            {
                DatabaseStatusResult.DatabaseNotFound => "Veritabanı dosyası belirtilen yolda bulunamadı.",
                DatabaseStatusResult.ConnectionFailed => "Dosyaya erişim sağlanıyor ancak bağlantı reddedildi.",
                DatabaseStatusResult.InvalidSchema => "Veritabanı yapısı bozulmuş veya eksik tablolar var.",
                DatabaseStatusResult.RequiredUpdating => IsEmptyDatabase
                    ? "Veritabanı boş, ilk kurulum yapılması gerekiyor."
                    : $"Veritabanı güncel değil. {PendingMigrations.Count} adet güncelleme uygulanmalı.",
                DatabaseStatusResult.RestoreCompleted => $"{DatabaseName} - Veritabanı yedekten başarıyla geri alındı",
                DatabaseStatusResult.Success => "Veritabanı sağlıklı ve tüm güncellemeler yapılmış.",
                _ => $"Analiz sırasında bir sorun tespit edildi: {Message}"
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