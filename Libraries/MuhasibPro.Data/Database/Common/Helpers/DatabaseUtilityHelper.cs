namespace MuhasibPro.Data.Database.Common.Helpers
{
    public static class DatabaseUtilityHelper
    {
        public static string GenerateTenantDatabaseName(string prefix, string firmaKodu, int maliYil)
        {
            // Tüm SQL adlarını büyük harf kullanmak ve özel karakterden kaçınmak iyi bir pratiktir.
            return $"{prefix}_{firmaKodu.ToUpper()}_{maliYil}";
        }

        public static string GenerateBackupFileName(string databaseName, string suffix = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var guidPart = Guid.NewGuid().ToString("N")[..4];

            var fileName = $"{databaseName}_{timestamp}_{guidPart}";
            if (!string.IsNullOrEmpty(suffix))
                fileName += $"_{suffix}";

            return fileName + ".backup";
        }
        public static string ExtractVersionFromMigration(string migrationName)
        {
            // Örnek: "20240115143000_InitialCreate" → "2024.01.15.1430"
            if (migrationName.Length >= 14 && migrationName.Take(14).All(char.IsDigit))
            {
                var ts = migrationName.AsSpan(0, 14);
                return $"{ts.Slice(0, 4)}.{ts.Slice(4, 2)}.{ts.Slice(6, 2)}.{ts.Slice(8, 4)}";
            }

            // Migration sayısına göre
            return $"1.0.0.{migrationName.GetHashCode() % 10000}";
        }
    }
}
