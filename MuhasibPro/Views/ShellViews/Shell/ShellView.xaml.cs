using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Helpers.WindowHelpers;
using MuhasibPro.HostBuilders;
using MuhasibPro.Services.ServiceExtensions;
using MuhasibPro.Services.UIService;
using MuhasibPro.ViewModels.ViewModels.Shell;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MuhasibPro.Views.ShellViews.Shell
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShellView : Page
    {
        public ShellView()
        {
            ViewModel = ServiceLocator.Current.GetService<ShellViewModel>();
            NotificationService = ServiceLocator.Current.GetService<INotificationService>();
            this.InitializeComponent();
            InitializeNavigation();
            InitializeContext();
            this.Loaded += OnLoaded;
        }

        public INotificationService NotificationService { get; }
        public ShellViewModel ViewModel { get; private set; }
        private void InitializeNavigation()
        {
            var navigationService = ServiceLocator.Current.GetService<INavigationService>();
            navigationService.Initialize(frame);
            var window = WindowHelper.CurrentWindow;
            window.Closed += Window_Closed;
        }
        public void InitializeContext()
        {
            var _contextService = ServiceLocator.Current.GetService<IContextService>();
            _contextService.InitializeWithContext(dispatcher: DispatcherQueue, viewElement: this);
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            if (ViewModel != null)
            {
                ViewModel.Unsubscribe();
                ViewModel = null;
                //Bindings.StopTracking();
            }
            var window = WindowHelper.GetWindowForElement(this);
            window.Closed -= Window_Closed;
            WindowHelper.MainWindow?.Activate();
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.LoadAsync(e.Parameter as ShellArgs);
            ViewModel.Subscribe();
        }
        private void OnUnlockClick(object sender, RoutedEventArgs e)
        {
            //InitializeContext();
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            NotificationService.InitializeNotificationService(ValidationInfoBar); if (ViewModel != null)
            {
                ViewModel.NotificationService = NotificationService;
            }
        }
    }
}
