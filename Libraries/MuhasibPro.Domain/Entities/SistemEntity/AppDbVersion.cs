using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.SistemEntity
{
    [Table("AppDbVersiyonlar")]
    public class AppDbVersion : AppVersion
    {
        public string DatabaseName { get; set; }
        public string CurrentDatabaseVersion { get; set; }
        public DateTime CurrentDatabaseLastUpdate { get; set; }
        public string? PreviousDatabaseVersion { get; set; }
    }
}
