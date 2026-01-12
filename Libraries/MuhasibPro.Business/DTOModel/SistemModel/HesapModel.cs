namespace MuhasibPro.Business.DTOModel.SistemModel;

public class HesapModel : ObservableObject
{
    public static HesapModel CreateEmpty()
        => new() { Id = -1, IsEmpty = true };

    public long KullaniciId { get; set; }
    public long? FirmaId { get; set; }    
    public KullaniciModel KullaniciModel { get; set; }
    public FirmaModel? FirmaModel { get; set; }
    public DateTime SonGirisTarihi { get; set; }    
    public bool IsNew => Id <= 0;

    public override void Merge(ObservableObject source)
    {
        if (source is HesapModel model)
            Merge(model);
    }

    public void Merge(HesapModel model)
    {
        if (model != null)
        {
            Id = model.Id;
            KullaniciId = model.KullaniciId;
            FirmaId = model.FirmaId;           
            KullaniciModel = model.KullaniciModel;
            FirmaModel = model.FirmaModel;
            SonGirisTarihi = model.SonGirisTarihi;

            KayitTarihi = model.KayitTarihi;
            GuncellemeTarihi = model.GuncellemeTarihi;
            KullaniciId = model.KullaniciId;
            GuncelleyenId = model.GuncelleyenId;
            AktifMi = model.AktifMi;
            

        }
    }

    public override string ToString()
    {
        return KullaniciId.ToString();
    }
}