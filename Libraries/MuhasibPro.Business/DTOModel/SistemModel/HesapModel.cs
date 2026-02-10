using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Business.DTOModel.SistemModel;

public class HesapModel 
{
    public static HesapModel CreateEmpty()
        => new() { KullaniciId = -1 };
    public static readonly HesapModel Default = new HesapModel
    {
        KullaniciModel = KullaniciModel.Default
    };
    public long KullaniciId { get; set; }
    public long? FirmaId { get; set; }    
    public KullaniciModel KullaniciModel { get; set; }
    public FirmaModel? FirmaModel { get; set; }
    public DateTime SonGirisTarihi { get; set; }    
 

 

    public void Merge(HesapModel model)
    {
        if (model != null)
        {
            KullaniciId = model.KullaniciId;
            FirmaId = model.FirmaId;           
            KullaniciModel = model.KullaniciModel;
            FirmaModel = model.FirmaModel;
            SonGirisTarihi = model.SonGirisTarihi;
        }
    }

    public override string ToString()
    {
        return KullaniciId.ToString();
    }
}