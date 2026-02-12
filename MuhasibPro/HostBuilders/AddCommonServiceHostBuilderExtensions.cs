using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Contracts.UIService;
using MuhasibPro.Services.CommonServices;
using MuhasibPro.Services.UIService;

namespace MuhasibPro.HostBuilders
{
    public static class AddCommonServiceHostBuilderExtensions
    {
        public static IHostBuilder AddCommonServices(this IHostBuilder host)
        {
            host.ConfigureServices(services =>
            {
                // ✅ GLOBAL & STATEFUL servisler - SINGLETON:
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<IActivationService, ActivationService>();
                //services.AddSingleton<IUpdateService, UpdateService>();
                //services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFilePickerService, FilePickerService>();
                services.AddSingleton<ISistemLogService, SistemLogService>();
                services.AddSingleton<IAppLogService, AppLogService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IMessageService, MessageService>();



                // ✅ VIEW/OPERATION başına - SCOPED:
                services.AddScoped<IStatusBarService, StatusBarService>();
                services.AddScoped<IStatusMessageService, StatusMessageService>();
                services.AddScoped<IBitmapToolsService, BitmapToolsService>();
                //services.AddScoped<IWebViewService, WebViewService>();
                services.AddSingleton<IStartupApplicationService, StartupApplicationService>();


                services.AddScoped<ICommonServices, CommonServices>();
                services.AddScoped<IContextService, ContextService>();
                services.AddScoped<INavigationService, NavigationService>();





            });
            return host;
        }
    }
}
