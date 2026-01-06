namespace MuhasibPro.Business.Contracts.SistemServices.LogServices
{
    public interface ILogService
    {
        public IAppLogService AppLogService { get; }
        public ISistemLogService SistemLogService { get; }
    }
}
