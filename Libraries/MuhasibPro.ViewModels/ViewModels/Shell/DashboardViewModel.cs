using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.ViewModels.Insrastructure.ViewModels;

namespace MuhasibPro.ViewModels.ViewModels.Shell
{
    public class DashboardViewModel : ViewModelBase
    {

        public DashboardViewModel(ICommonServices commonServices) : base(commonServices)
        {
        }
    }
}
