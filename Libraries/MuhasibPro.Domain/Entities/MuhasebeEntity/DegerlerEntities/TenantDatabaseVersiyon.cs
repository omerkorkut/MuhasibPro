using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities
{
    [Table("TenantDatabaseVersiyonlar")]
    public class TenantDatabaseVersiyon
    {
        [Key]
        public string DatabaseName { get; set; }
        public string CurrentTenantDbVersion { get; set; }
        public DateTime CurrentTenantDbLastUpdate { get; set; }
        public string? PreviousTenantDbVersiyon { get; set; }

    }
}

