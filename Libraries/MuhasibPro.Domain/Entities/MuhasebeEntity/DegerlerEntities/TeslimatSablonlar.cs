using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities
{
    [Table("TeslimatSablonlar")]
    public class TeslimatSablonlar : BaseEntity
    {
        public string SablonAdi { get; set; }

    }
}
