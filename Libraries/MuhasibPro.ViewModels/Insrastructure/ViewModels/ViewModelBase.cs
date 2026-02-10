using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel;
using MuhasibPro.Domain.Enum;

namespace MuhasibPro.ViewModels.Insrastructure.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
        public ViewModelBase(ICommonServices commonServices)
        {
            ContextService = commonServices.ContextService;
            NavigationService = commonServices.NavigationService;
            MessageService = commonServices.MessageService;
            DialogService = commonServices.DialogService;
            LogService = commonServices.LogService;
            NotificationService = commonServices.NotificationService;
            StatusMessageService = commonServices.StatusMessageService;
        }

        public IContextService ContextService { get; private set; }

        public INavigationService NavigationService { get; private set; }

        public IMessageService MessageService { get; }

        public IDialogService DialogService { get; }

        public ILogService LogService { get; }

        public INotificationService NotificationService { get; set; }

        public IStatusBarService StatusBarService { get; set; }

        public IStatusMessageService StatusMessageService { get; set; }

        public bool IsMainWindow => ContextService.IsMainView;

        private bool _isBusy = false;

        public virtual bool IsBusy { get => _isBusy; set => Set(ref _isBusy, value); }

        public virtual string Title => string.Empty;

        #region Log Metodları
        public async Task LogAppInformationAsync(string source, string action, string message, string description)
        {
            try
            {
                await LogService.AppLogService.WriteAsync(LogType.Bilgi, source, action, message, description);
            } catch
            { /* Logging hatası sessizce ignore */
            }
        }

        public async Task LogSistemInformationAsync(string source, string action, string message, string description)
        {
            try
            {
                await LogService.SistemLogService.WriteAsync(LogType.Bilgi, source, action, message, description);
            } catch
            { /* Logging hatası sessizce ignore */
            }
        }

        public async Task LogAppWarningAsync(string source, string action, string message, string description)
        {
            try
            {
                await LogService.AppLogService.WriteAsync(LogType.Dikkat, source, action, message, description);
            } catch
            { /* Logging hatası sessizce ignore */
            }
        }

        public async Task LogSistemWarningAsync(string source, string action, string message, string description)
        {
            try
            {
                await LogService.SistemLogService.WriteAsync(LogType.Dikkat, source, action, message, description);
            } catch
            { /* Logging hatası sessizce ignore */
            }
        }

        public async Task LogAppExceptionAsync(string source, string action, Exception exception)
        { await LogAppErrorAsync(source, action, exception.Message, exception.ToString()); }

        public async Task LogSistemExceptionAsync(string source, string action, Exception exception)
        { await LogSistemErrorAsync(source, action, exception.Message, exception.ToString()); }

        public async Task LogAppErrorAsync(string source, string action, string message, string description)
        {
            try
            {
                await LogService.AppLogService.WriteAsync(LogType.Hata, source, action, message, description);
            } catch
            { /* Logging hatası sessizce ignore */
            }
        }

        public async Task LogSistemErrorAsync(string source, string action, string message, string description)
        {
            try
            {
                await LogService.SistemLogService.WriteAsync(LogType.Hata, source, action, message, description);
            } catch
            { /* Logging hatası sessizce ignore */
            }
        }
        #endregion      

        #region View Enable/Disable Metodları
        public void EnableThisView(string message = null)
        {
            message = message ?? "Hazır";
            MessageService.Send(this, "EnableThisView", message);
        }

        public void DisableThisView(string message) { MessageService.Send(this, "DisableThisView", message); }

        public void EnableOtherViews(string message = null)
        {
            message = message ?? "Hazır";
            MessageService.Send(this, "EnableOtherViews", message);
        }

        public void DisableOtherViews(string message) { MessageService.Send(this, "DisableOtherViews", message); }

        public void EnableAllViews(string message = null)
        {
            message = message ?? "Hazır";
            MessageService.Send(this, "EnableAllViews", message);
        }

        public void DisableAllViews(string message) { MessageService.Send(this, "DisableAllViews", message); }
        #endregion

        #region Simple Status Message Wrappers
        public void StatusReady() => StatusMessageService.Clear();

        public void StatusActionMessage(string message, StatusMessageType type, int autoHide) => StatusMessageService.ShowMessage(
            message,
            type,
            autoHide);

        #region  Error Exception Global Mesaj
        public void StatusError(string message, StatusMessageType type = StatusMessageType.Error, int autoHide = -1)
        { StatusActionMessage(message, type, autoHide); }
        #endregion

        #endregion

        #region Progress Wrappers
        public void StartProgressWithPercent(string message = "İşlem yapılıyor...", double percent = -1) => StatusMessageService.ShowProgress(
            message,
            percent);

        public void UpdateProgress(double percent) => StatusMessageService.UpdateProgress(percent);

        public void StopProgress() => StatusMessageService.HideProgress();
        #endregion

        #region Advanced Async Operations - BUNLAR VIEWMODEL'LERDE KULLANILACAK
        public async Task ExecuteActionWithProgressAsync(
            Func<Task> action,
            string progressMessage,
            string successMessage = null,
            string errorMessage = null,
            bool measureTime = true,
            int successAutoHideSeconds = 3)
        {
            await StatusMessageService.ExecuteWithProgressAsync(
                action,
                progressMessage,
                successMessage,
                errorMessage,
                measureTime,
                successAutoHideSeconds);
        }

        public async Task ExecuteWithProgressAsync(
            Func<IProgress<double>, Task> action,
            string progressMessage,
            string successMessage = null,
            string errorMessage = null,
            bool measureTime = true,
            int successAutoHideSeconds = 3)
        {
            await StatusMessageService.ExecuteWithProgressAsync(
                action,
                progressMessage,
                successMessage,
                errorMessage,
                measureTime,
                successAutoHideSeconds);
        }

        public async Task ExecuteActionAsync(
            Func<Task> action,
            string startMessage = null,
            StatusMessageType startMessageType = StatusMessageType.Info,
            string successMessage = null,
            string errorMessage = null,
            bool measureTime = true,
            int successAutoHideSeconds = 3)
        {
            await StatusMessageService.ExecuteActionAsync(
                action,
                startMessage,
                startMessageType,
                successMessage,
                errorMessage,
                measureTime,
                successAutoHideSeconds);
        }
        #endregion
    }
}