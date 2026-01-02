using MuhasibPro.Data.Contracts.Database.Common.Helpers;

namespace MuhasibPro.Data.Database.Common.Helpers
{
    public class EnvironmentDetector : IEnvironmentDetector
    {
        public bool IsDevelopment()
        {
            // DEBUG = Geliştirme = Proje klasörü
            // RELEASE = Kullanım = AppData
#if DEBUG
            return true;
#else
    return false;
#endif
        }

        public bool IsProduction() => !IsDevelopment();
    }
}
