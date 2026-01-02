namespace MuhasibPro.Domain.Enum.DatabaseEnum
{
    public enum DatabaseBackupType
    {
        Manual = 1,      // Kullanıcı manuel oluşturdu
        Automatic = 2,   // Schedule/otomatik backup
        Safety = 3,      // Restore öncesi güvenlik
        Migration = 4,   // Migration öncesi
        System = 5 
    }
}
