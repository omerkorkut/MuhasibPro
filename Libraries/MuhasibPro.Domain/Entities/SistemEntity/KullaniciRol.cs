using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.SistemEntity
{
    [Table("KullaniciRoller")]
    public class KullaniciRol : BaseEntity
    {
        public string RolAdi { get; set; }
        public string Aciklama { get; set; }
    }
}
