// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler;

namespace MuhasibPro.Views.MaliDonem
{
    public sealed partial class MaliDonemOlusturProgress : UserControl
    {
        public MaliDonemOlusturProgress()
        {
            InitializeComponent();
        }
        #region ViewModel
        public MaliDonemCreationViewModel CreationViewModel
        {
            get { return (MaliDonemCreationViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty = 
            DependencyProperty.Register(
                nameof(CreationViewModel), 
                typeof(MaliDonemCreationViewModel), 
                typeof(MaliDonemOlusturProgress),
                new PropertyMetadata(null));
        #endregion
    }
}
