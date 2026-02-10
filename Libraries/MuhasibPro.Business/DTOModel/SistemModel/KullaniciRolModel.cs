using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Business.DTOModel.SistemModel
{
    public class KullaniciRolModel : ObservableObject
    {
        public string RolAdi { get; set; }
        public string Aciklama { get; set; }
        public KullaniciRolTip RolTip { get; set; }
    }
}
