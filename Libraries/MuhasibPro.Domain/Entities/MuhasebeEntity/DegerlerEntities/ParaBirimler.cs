using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities
{
    [Table("ParaBirimler")]
    public class ParaBirimler : BaseEntity
    {
        public string Kisaltmasi { get; set; }
        public string PB { get; set; }
    }
}