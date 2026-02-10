using MuhasibPro.Business.Contracts.SistemServices.LogServices;

namespace MuhasibPro.Business.Contracts.UIServices.CommonServices
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
        IStatusMessageService StatusMessageService { get; }
    }
}
