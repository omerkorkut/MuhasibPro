namespace MuhasibPro.Data.Contracts.Database.Common.Helpers
{
    public interface IEnvironmentDetector
    {
        bool IsDevelopment();
        bool IsProduction();
    }
}
