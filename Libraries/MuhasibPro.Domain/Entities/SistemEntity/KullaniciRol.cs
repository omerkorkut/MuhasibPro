using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.SistemEntity
{
    [Table("KullaniciRoller")]
    public class KullaniciRol : BaseEntity
    {
        public string RolAdi { get; set; }
        public string Aciklama { get; set; }
        public KullaniciRolEnum RolTuru { get; set; }
    }
    public enum KullaniciRolEnum
    {
        Admin = 1,
        User = 2,
        Guest = 3
    }
}
