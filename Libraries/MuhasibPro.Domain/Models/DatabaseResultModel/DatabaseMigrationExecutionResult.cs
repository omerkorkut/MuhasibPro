using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseMigrationExecutionResult : BaseDatabaseResult
    {
        public int AppliedMigrationsCount { get; set; }
        public List<string> PendingMigrations { get; set; } = new();
        public bool DatabaseValid { get; set; }
        public bool BackupTaken { get; set; } // ✅ YENİ: Backup alındı mı?
        public override bool IsUpdateRequired => PendingMigrations.Any();
        public bool IsRolledBack { get; set; }
        public override string OperationDisplayName => "Güncelleme";

        public override DatabaseStatusResult GetStatus()
        {
            if (HasError) return DatabaseStatusResult.UnknownError;
            if (!CanConnect) return DatabaseStatusResult.ConnectionFailed;
            if (!DatabaseValid) return DatabaseStatusResult.InvalidSchema;
            if (IsRolledBack) return DatabaseStatusResult.RestoreCompleted;

            return IsUpdateRequired ? DatabaseStatusResult.RequiredUpdating : DatabaseStatusResult.Healty;
        }

        public override string GetStatusMessage()
        {
            if (HasError)
            {
                if (IsRolledBack)
                    return $"Kritik bir hata oluştu: {Message}. Sistem otomatik olarak yedekten geri yüklendi.";

                return $"Güncelleme başarısız: {Message}. Lütfen manuel bir yedek kontrolü yapın.";
            }

            string baseMessage = IsUpdateRequired
                ? $"{AppliedMigrationsCount} yeni güncelleme başarıyla veritabanına işlendi."
                : "Veritabanı zaten en son sürümde, işlem yapılmadı.";

            // Backup alındıysa ekle
            if (BackupTaken)
            {
                baseMessage += " (Yedek alındı)";
            }

            return baseMessage;
        }

        // ✅ YENİ: Migration uygulandı mı?
        public bool MigrationApplied => AppliedMigrationsCount > 0 && !HasError;

        // ✅ YENİ: İşlem başarılı mı?
        public bool IsSuccess => !HasError && CanConnect && DatabaseValid;
    }
}
