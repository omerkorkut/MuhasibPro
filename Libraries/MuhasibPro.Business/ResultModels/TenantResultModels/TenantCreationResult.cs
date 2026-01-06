namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public class TenantCreationResult
    {
        public string DatabaseName { get; set; }
        public long MaliDonemId { get; set; }
        public bool DatabaseCreated { get; set; }
        public bool MigrationsRun { get; set; }
        public string Message { get; set; }
    }
}
