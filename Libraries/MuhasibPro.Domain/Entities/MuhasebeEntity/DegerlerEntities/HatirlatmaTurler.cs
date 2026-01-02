using System.ComponentModel.DataAnnotations;

namespace MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities
{
    public class HatirlatmaTurler : BaseEntity
    {
        [MaxLength(50)] public string TurAdi { get; set; }
        public ICollection<Hatirlatmalar> Hatirlatmalar { get; set; }
    }
}