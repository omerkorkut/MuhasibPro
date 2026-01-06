using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Business.EntityModel.SistemModel
{
    public class KullaniciRolModel : ObservableObject
    {
        public string RolAdi { get; set; }
        public string Aciklama { get; set; }
        public KullaniciRolEnum RolTuru { get; set; }
    }
}
