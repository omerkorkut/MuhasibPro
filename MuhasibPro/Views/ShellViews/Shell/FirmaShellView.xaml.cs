// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using MuhasibPro.HostBuilders;
using MuhasibPro.ViewModels.ViewModels.Shell;
using System.Threading.Tasks;

namespace MuhasibPro.Views.ShellViews.Shell
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirmaShellView : Page
    {
        public FirmaShellView()
        {
            InitializeComponent();
            ViewModel = ServiceLocator.Current.GetService<FirmaShellViewModel>();

        }
        public FirmaShellViewModel ViewModel { get;}
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as ShellArgs ?? new ShellArgs();
            await ViewModel.LoadAsync(args);
            ViewModel.BaseSubscribe();
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.BaseUnsubscribe();
            ViewModel.Unload();
        }
    }
}
