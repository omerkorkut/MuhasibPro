using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.DTOModel;
using MuhasibPro.Business.Services.SistemServices.AppServices;
using MuhasibPro.Business.Services.SistemServices.Authetication;
using MuhasibPro.Business.Services.SistemServices.LogServices;

namespace MuhasibPro.Business.HostBuilder
{
    public static class AddServicesHostBuilderExtensions
    {
        public static IHostBuilder AddBusinessServices(this IHostBuilder host)
        {
            host.ConfigureServices(services =>
            {
                services.AddSingleton<ModelFactory>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IFirmaService, FirmaService>();
                services.AddSingleton<IMaliDonemService, MaliDonemService>();
                services.AddSingleton<IFirmaWithMaliDonemSelectedService, FirmaWithMaliDonemSelectedService>();
                                
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<IAppLogService, AppLogService>();
                services.AddSingleton<ISistemLogService, SistemLogService>();

                
            });
            return host;
        }
    }
}
