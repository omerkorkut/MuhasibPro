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
            try
            {
                if (string.IsNullOrEmpty(migrationName))
                    return "1.0.0.0"; // İlk versiyon

                var parts = migrationName.Split('_', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                    return "1.0.0.0";

                var timestampPart = parts[0];

                // Alternatif: Daha kısa format
                if (timestampPart.Length == 14 && long.TryParse(timestampPart, out var timestamp))
                {
                    // Sadece gün ve saat: 20240115143000 -> 1.1.15.1430
                    var day = timestampPart.Substring(6, 2); // 15
                    var hourMinute = timestampPart.Substring(8, 4); // 1430

                    return $"1.1.{day}.{hourMinute}"; // Major.Minor.Day.HourMinute
                }

                // Timestamp değilse
                var hash = CalculateSimpleHash(migrationName);
                return $"1.0.0.{hash}";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        private static string CalculateSimpleHash(string input)
        {
            // Basit bir hash (CRC32 veya basit checksum)
            unchecked
            {
                int hash = 17;
                foreach (char c in input)
                {
                    hash = hash * 31 + c;
                }
                return Math.Abs(hash).ToString("X8").Substring(0, 6);
            }
        }
    }
}
