using MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.HostBuilders;
using MuhasibPro.Services.CommonServices;
using MuhasibPro.ViewModels.ViewModels.Shell;
using MuhasibPro.ViewModels.ViewModels.Sistem.Firmalar;
using MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler;
using MuhasibPro.Views.Firma;
using MuhasibPro.Views.Firmalar;
using MuhasibPro.Views.Login;
using MuhasibPro.Views.MaliDonem;
using MuhasibPro.Views.ShellViews.Shell;

namespace MuhasibPro.Configurations
{
    public class Startup
    {
        private static readonly Lazy<Startup> _instance = new Lazy<Startup>(() => new Startup());

        public static Startup Instance => _instance.Value;

       
        private Startup()
        {
        }

        public async Task ConfigureAsync()
        {
            ConfigureNavigation();
            await InitializeSistemDatabase();
        }

        public void ConfigureNavigation()
        {
            NavigationService.Register<LoginViewModel, LoginView>();
            NavigationService.Register<ShellViewModel, ShellView>();
            //NavigationService.Register<MainShellViewModel, MainShellView>();
            NavigationService.Register<FirmaShellViewModel,FirmaShellView>();
            
            

            //NavigationService.Register<DashboardViewModel, DashboardView>();
            //NavigationService.Register<SettingsViewModel, SettingsView>();
            //NavigationService.Register<UpdateViewModel, UpdateView>();
            //NavigationService.Register<SistemLogsViewModel, SistemLogsView>();

            NavigationService.Register<FirmaDetailsViewModel, FirmaView>();
            NavigationService.Register<FirmalarViewModel, FirmalarView>();

            NavigationService.Register<MaliDonemDetailsViewModel, MaliDonemView>();
        }
        
        public async Task<(bool isValid, string message)> InitializeSistemDatabase()
        {
            try
            {
                var sistemDbService = ServiceLocator.Current.GetService<ISistemDatabaseService>();
                var statusBarService = ServiceLocator.Current.GetService<IStatusBarService>();

                var initilize = await sistemDbService.InitializeSistemDatabaseAsync();

                if (!initilize.intializeState)
                {
                    return (false, message: initilize.message);
                }

                await sistemDbService.InitializeSistemDatabaseAsync();
                statusBarService.DatabaseConnectionMessage = initilize.message;
                
                return (true, message: initilize.message);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

    }


}
