namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public class TenantDeletingRequest
    {
        public long MaliDonemId { get; set; }
        public bool IsDeleteDatabase { get; set; }
        public bool IsDeleteMaliDonem { get; set; }
        public string DatabaseName { get; set; }
        public bool DeleteAllTenantBackup { get; set; }       
        public bool  IsCurrentTenantDeletingBeforeBackup { get; set; }
    }
}
