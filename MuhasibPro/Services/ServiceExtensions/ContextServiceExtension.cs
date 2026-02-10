using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Helpers.WindowHelpers;

namespace MuhasibPro.Services.ServiceExtensions
{
    public static class ContextServiceExtension
    {
        /// <summary>
        /// ContextService'i initialize eder ve ViewContext'i kaydeder
        /// </summary>
        public static void InitializeWithContext(
            this IContextService contextService,
            object dispatcher,
            FrameworkElement viewElement)
        {
            var window = WindowHelper.GetWindowForElement(viewElement);
            var contextId = WindowHelper.GetWindowId(window);
            var mainViewId = WindowHelper.GetWindowId(WindowHelper.MainWindow);
            var isMainView = contextId == mainViewId;

            // ✅ TEK SATIR - sadece orijinal metodu çağır
            contextService.Initialize(dispatcher, contextId, isMainView);
        }
    }
}