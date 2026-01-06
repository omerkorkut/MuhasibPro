using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.SistemEntity;

[Table("Hesaplar")]
public class Hesap : BaseEntity
{
    public long KullaniciId { get; set; }
    public long? FirmaId { get; set; }    
    public DateTime SonGirisTarihi { get; set; }
    public Kullanici Kullanici { get; set; }
}


