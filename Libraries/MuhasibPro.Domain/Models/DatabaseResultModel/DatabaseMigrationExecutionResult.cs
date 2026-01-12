using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseMigrationExecutionResult : BaseDatabaseResult
    {
        public int AppliedMigrationsCount { get; set; }
        public List<string> PendingMigrations { get; set; } = new();
        public bool DatabaseValid { get; set; }
        public override bool IsUpdateRequired => PendingMigrations.Any();
        public bool IsRolledBack { get; set; }
        public override string OperationDisplayName => "Güncelleme";

        public override DatabaseStatusResult GetStatus()
        {
            if (HasError) return DatabaseStatusResult.UnknownError;
            if (!CanConnect) return DatabaseStatusResult.ConnectionFailed;
            if (!DatabaseValid) return DatabaseStatusResult.InvalidSchema;

            return DatabaseStatusResult.Healty;
        }

        public override string GetStatusMessage()
        {
            if (HasError)
            {
                // Eğer hata varsa ve sistem otomatik geri yükleme yaptıysa:
                if (IsRolledBack)
                    return $"Kritik bir hata oluştu: {Message}. Sistem otomatik olarak kararlı yedek durumuna geri döndürüldü. (Safe Mode)";

                return $"Güncelleme başarısız: {Message}. Lütfen manuel bir yedek kontrolü yapın.";
            }

            return AppliedMigrationsCount > 0
                ? $"{AppliedMigrationsCount} yeni güncelleme başarıyla veritabanına işlendi."
                : "Veritabanı zaten en son sürümde, işlem yapılmadı.";
        }
    }
}