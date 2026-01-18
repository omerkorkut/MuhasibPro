namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public class TenantDetailsModel
    {
        public long MaliDonemId { get; set; }
        public long FirmaId { get; set; }
        public string FirmaKodu { get; set; }
        public string FirmaKisaUnvan {  get; set; }        
        public int MaliYil {  get; set; }
        public long UserId { get; set; }
        public string DatabaseName { get; set; }        
    }
}
