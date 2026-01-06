using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;

namespace MuhasibPro.Business.Contracts.CommonServices
{
    public interface ICommonServices
    {
        IContextService ContextService { get; }
        INavigationService NavigationService { get; }
        IMessageService MessageService { get; }
        IDialogService DialogService { get; }
        ILogService LogService { get; }
        INotificationService NotificationService { get; }
        ISettingsService SettingsService { get; }
        IStatusBarService StatusBarService { get; }
    }
}
