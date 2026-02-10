using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;

namespace MuhasibPro.Services.CommonServices;
public class CommonServices : ICommonServices
{
    public CommonServices(
        IContextService contextService,
        INavigationService navigationService,
        IMessageService messageService,
        IDialogService dialogService,
        ILogService logService,
        INotificationService notificationService,
        
        IStatusBarService statusBarService,
        IStatusMessageService statusMessageService)
    {
        ContextService = contextService;
        NavigationService = navigationService;
        MessageService = messageService;
        DialogService = dialogService;
        LogService = logService;
        NotificationService = notificationService;
       
        StatusBarService = statusBarService;
        StatusMessageService = statusMessageService;
    }
    public IContextService ContextService { get; }
    public INavigationService NavigationService { get; }
    public IMessageService MessageService { get; }
    public IDialogService DialogService { get; }
    public ILogService LogService { get; }
    public INotificationService NotificationService { get; }
    public ISettingsService SettingsService { get; }
    public IStatusBarService StatusBarService { get; }
    public IStatusMessageService StatusMessageService { get; }
}
