// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler;

namespace MuhasibPro.Views.MaliDonem
{
    public sealed partial class MaliDonemSilProgress : UserControl
    {
        public MaliDonemSilProgress()
        {
            InitializeComponent();
        }
        #region ViewModel
        public MaliDonemDeletionViewModel DeletionViewModel
        {
            get { return (MaliDonemDeletionViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(DeletionViewModel),
                typeof(MaliDonemDeletionViewModel),
                typeof(MaliDonemSilProgress),
                new PropertyMetadata(null));
        #endregion
    }
}
