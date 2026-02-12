using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.Globalization;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.HostBuilder;
using MuhasibPro.Contracts.UIService;
using MuhasibPro.Domain.Enum;
using MuhasibPro.Domain.Helpers;
using MuhasibPro.Helpers.WindowHelpers;
using MuhasibPro.HostBuilders;
using System.Diagnostics;
using System.Globalization;
using Velopack;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MuhasibPro
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public static DispatcherQueue _dispatcherQueue;

        private static Window MainWindow => new MainWindow();

        public static IThemeSelectorService ThemeSelectorService => ServiceLocator.Current
            .GetService<IThemeSelectorService>();

        public static UIElement? AppTitleBar { get; set; }

        public App()
        {
            this.InitializeComponent();
            CultureInitialize();
            _host = CreateHostBuilder().Build();
            ServiceLocatorStart(_host);


            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            this.UnhandledException += OnUnhandledException;
        }

        private void CultureInitialize()
        {
            var cultures = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultures;
            CultureInfo.DefaultThreadCurrentUICulture = cultures;
            ApplicationLanguages.PrimaryLanguageOverride = cultures.IetfLanguageTag;
        }

        public static void ServiceLocatorStart(IHost host) { ServiceLocator.Configure(host.Services); }

        public static void  VelopackInitialize()
        {
            VelopackApp.Build()
                .OnFirstRun(
                    v =>
                    {
                    })
                .OnRestarted(
                    v =>
                    {
                    })
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args = null)
        {
            return Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .AddConfiguration()
                .AddDatabaseManagement()
                .AddRepositories()
                .AddBusinessServices()
                .AddCommonServices()
                .AddAppViewModel()
                //.AddAppView();
                ;
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                WindowHelper.SetMainWindow(MainWindow);
                _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                await ActiveteAsync(args);
            } catch(Exception ex)
            {
                //await ActiveteAsync(args);
                await ShowNotificationAsync(ex.Message);
            }
        }

        private async Task ActiveteAsync(LaunchActivatedEventArgs args)
        {
            var activationService = _host.Services.GetRequiredService<IActivationService>();
            if(activationService != null)
            {
                await activationService.ActivateAsync(args);
            } else
            {
                WindowHelper.MainWindow?.Activate();
            }
        }

        public static async Task ShowNotificationAsync(string message, string title = "Bilgi")
        {
            try
            {
                // UI thread'de çalıştır
                if(_dispatcherQueue != null)
                {
                    var taskCompletionSource = new TaskCompletionSource<bool>();
                    _dispatcherQueue.TryEnqueue(
                        async () =>
                        {
                            try
                            {
                                if(MainWindow?.Content?.XamlRoot != null)
                                {
                                    var dialog = new ContentDialog
                                    {
                                        Title = title,
                                        Content = message,
                                        PrimaryButtonText = "Tamam",
                                        XamlRoot = MainWindow.Content.XamlRoot
                                    };
                                    await dialog.ShowAsync();
                                }
                                taskCompletionSource.SetResult(true);
                            } catch(Exception ex)
                            {
                                Debug.WriteLine($"Notification failed: {ex.Message}");
                                taskCompletionSource.SetResult(false);
                            }
                        });
                    await taskCompletionSource.Task;
                }
            } catch(Exception ex)
            {
                Debug.WriteLine($"Notification failed: {ex.Message}");
            }
        }

        public ILogService LogService => _host.Services.GetRequiredService<ILogService>();

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // KRİTİK: Exception'ı handled olarak işaretle - uygulama çökmeyecek
            e.Handled = true;
            Debug.WriteLine($"Unhandled exception (handled): {e.Exception}");
            LogService.SistemLogService.WriteAsync(LogType.Hata, this.ToString(), e.Message, e.Exception);
            // UI thread'de hata göster
            if(_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(
                    async () =>
                    {
                        await ShowNotificationAsync(
                            "Beklenmedik bir hata oluştu, ancak uygulama çalışmaya devam ediyor.",
                            "Sistem Hatası");
                    });
            }
        }
    }
}

