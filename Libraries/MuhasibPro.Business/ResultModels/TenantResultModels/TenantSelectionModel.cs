using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public class TenantSelectionModel
    {
        public long MaliDonemId { get; set; }
        public long UserId { get; set; }
        public long FirmaId { get; set; }
        public string FirmaKodu { get; set; }
        public string FirmaKisaUnvani { get; set; }
        public int MaliYil { get; set; }
        public string DatabaseName { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public bool AktifMi { get; set; }
        public string DisplayText => $"{FirmaKisaUnvani} - {MaliYil}";
    }
}
