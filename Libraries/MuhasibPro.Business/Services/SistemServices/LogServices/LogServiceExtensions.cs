using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Domain.Enum;

namespace MuhasibPro.Business.Services.SistemServices.LogServices
{
    public static class LogServiceExtensions
    {
        //ISistemLogService extensions methods
        public static async Task SistemLogInformationAsync(this ISistemLogService sistemLogService, string source, string action, string message, string description)
        { await sistemLogService.WriteAsync(LogType.Bilgi, source, action, message, description); }
        public static async Task SistemLogErrorAsync(this ISistemLogService sistemLogService, string source, string action, string message, string description = "")
        { await sistemLogService.WriteAsync(LogType.Hata, source, action, message, description); }
        public static async Task SistemLogExceptionAsync(this ISistemLogService sistemLogService, string source, string action, Exception exception)
        { await sistemLogService.WriteAsync(LogType.Hata, source, action, exception.Message, exception.ToString()); }

        // IAppLogService extension methods

        public static async void AppLogInformationAsync(this IAppLogService appLogService, string source, string action, string message, string description)
        { await appLogService.WriteAsync(LogType.Bilgi, source, action, message, description); }
        public static async void AppLogErrorAsync(this IAppLogService appLogService, string source, string action, Exception exception)
        { await appLogService.WriteAsync(LogType.Hata, source, action, exception.Message, exception.ToString()); }
        public static async void AppLogExceptionAsync(this IAppLogService appLogService, string source, string action, Exception exception)
        { await appLogService.WriteAsync(LogType.Hata, source, action, exception.Message, exception.ToString()); }
    }
}
