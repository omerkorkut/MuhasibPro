using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Contracts.UIService;
using MuhasibPro.Helpers;
using MuhasibPro.Helpers.WindowHelpers;
using MuhasibPro.ViewModels.ViewModels.Shell;
using MuhasibPro.Views.ShellViews.Splash;
using System.Diagnostics;

namespace MuhasibPro.Services.UIService
{
    public class ActivationInfo()
    {
        public static ActivationInfo CreateDefault() => Create<DashboardViewModel>();
        public static ActivationInfo Create<TViewModel>(object entryArgs = null) where TViewModel : class
        {
            return new ActivationInfo
            { EntryViewModel = typeof(TViewModel), EntryArgs = entryArgs };
        }
        public Type EntryViewModel { get; set; }
        public object EntryArgs { get; set; }
    }
    public class ActivationService : IActivationService
    {
        private readonly IThemeSelectorService _themeSelectorService;
        private readonly IStartupApplicationService _startupApplicationService;
        private UIElement? _shell = null;
        private ExtendedSplash _extendedSplash;

        public ActivationService(IThemeSelectorService themeSelectorService, IStartupApplicationService startupApplicationService)
        {
            _themeSelectorService = themeSelectorService;
            _startupApplicationService = startupApplicationService;
        }
        public async Task ActivateAsync(object activationArgs)
        {

            try
            {
                // Execute tasks before activation.
                await InitializeAsync();
                // Set the MainWindow Content.
                if (WindowHelper.MainWindow.Content == null)
                {
                    WindowHelper.MainWindow.Content = _shell ?? new Frame();
                }
                if (WindowHelper.MainWindow.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = _themeSelectorService.Theme;
                    _extendedSplash = new ExtendedSplash();
                    _extendedSplash.RequestedTheme = _themeSelectorService.Theme;
                    TitleBarHelper.UpdateTitleBar(rootElement.RequestedTheme);
                }
                // Execute tasks after activation.
                await StartupAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Theme setting failed: {ex.Message}");
            }
        }



        private async Task InitializeAsync()
        {
           
            await _themeSelectorService.InitializeAsync();           
        }

        private async Task StartupAsync()
        {
            await _themeSelectorService.SetRequestedThemeAsync();
            StartSplashScreenAsync();
        }
        private async void StartSplashScreenAsync()
        {
            var frame = WindowHelper.MainWindow.Content as Frame;

            if (frame != null)
            {
                // 2. Window'a set et
                WindowHelper.MainWindow.ExtendsContentIntoTitleBar = true;
                WindowHelper.MainWindow.Content = _extendedSplash;
                WindowHelper.MainWindow.Activate();
                // 3. UI'ın yüklenmesini bekle
                await Task.Delay(100); // UI'ın render olması için kısa bekle

            }
        }
    }
}
