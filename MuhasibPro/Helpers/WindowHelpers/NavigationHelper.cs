using MuhasibPro.Contracts.UIService;
using MuhasibPro.Extensions;
using MuhasibPro.HostBuilders;
using MuhasibPro.Services.ServiceExtensions;
using MuhasibPro.ViewModels.ViewModels.Shell;
using MuhasibPro.Views;
using MuhasibPro.Views.ShellViews.Shell;

namespace MuhasibPro.Helpers.WindowHelpers
{
    public class NavigationHelper
    {
        private static readonly Lazy<NavigationHelper> _instance = new(() => new NavigationHelper());

        public static NavigationHelper Instance => _instance.Value;

        private static Window MainWindow => WindowHelper.MainWindow;

        private async Task SetupContentAsync(Window window, Type viewModelType, object parameter)
        {
            await window.DispatcherQueue
                .EnqueueAsync(
                    () =>
                    {
                        var frame = new Frame();
                        var args = new ShellArgs { ViewModel = viewModelType, Parameter = parameter };
                        frame.Navigate(typeof(ShellView), args);
                        window.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
                        window.Content = frame;
                    });
        }

        public async Task CloseWindowAsync(int windowId)
        {
            var window = WindowHelper.GetWindowById(windowId);
            if(window == null || window == MainWindow)
                return;

            try
            {
                if(window.Content is Frame frame && frame.Content is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                await window.DispatcherQueue.EnqueueAsync(() => window.Close());
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Window close error: {ex.Message}");
            }
        }
        public async Task<int> CreateNewWindowAsync(Type viewModelType, object parameter, string customTitle)
        {
            int viewId = -1;
            try
            {
                if (WindowHelper.TryActivateExistingWindow(viewModelType))
                {                    
                    return await TryExistWindowFromViewModel(viewModelType);
                }

                var detailWindow = new DetailsWindow();
                detailWindow.AppWindow.Title = customTitle;
                viewId = (int)detailWindow.AppWindow.Id.Value;

                if(viewModelType != null)
                {
                    WindowPosition.PositionRelativeToParent(detailWindow, MainWindow);
                    WindowHelper.RegisterWindow(detailWindow, viewId, viewModelType);
                    await SetupContentAsync(detailWindow, viewModelType,parameter);
                    ApplyTheme(detailWindow);
                    return viewId;
                }
                return viewId;

            }
            catch (Exception)
            {
                return viewId;
            }
        }

        private void ApplyTheme(Window window)
        {
            var themeService = ServiceLocator.Current.GetService<IThemeSelectorService>();
            if(themeService != null)
            {
                themeService.ApplyThemeToWindow(window);
                themeService.SubscribeToThemeChanges(window);
            }
        }

        private async Task<int> TryExistWindowFromViewModel(Type viewModelType)
        {
            var existingWindow = WindowHelper.GetWindowByViewModel(viewModelType);
            var windowId = WindowHelper.GetWindowId(existingWindow);
            return  await Task.FromResult(windowId);
        }
    }
}
