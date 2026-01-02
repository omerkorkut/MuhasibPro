namespace MuhasibPro.Data.Database.Common.Helpers
{
    public static class DatabaseNamingHelper
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
    }
}
