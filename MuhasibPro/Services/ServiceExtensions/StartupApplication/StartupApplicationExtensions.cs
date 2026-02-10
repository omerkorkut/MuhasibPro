using MuhasibPro.Configurations;
using MuhasibPro.Contracts.UIService;

namespace MuhasibPro.Services.ServiceExtensions.StartupApplication
{
    public static partial class StartupApplicationExtensions
    {

        /// <summary>
        /// Bir startup step'ini yürütür
        /// </summary>
        public static async Task ExecuteStepAsync(
            this IStartupApplicationService startupService,
            StartupStep step,
            string stepName,
            Func<Task> action,
            CancellationToken cancellationToken = default)
        {
            // Step başlat
            await startupService.BeginStepAsync(step, $"{stepName} başlatılıyor...", cancellationToken);

            await Task.Delay(100, cancellationToken);

            try
            {
                // Gerçek işi yap
                await action();

                // Step tamamla
                await startupService.CompleteStepAsync($"{stepName} tamamlandı", cancellationToken);
            } catch(Exception ex)
            {
                // Hata durumu
                await startupService.FailStepAsync($"{stepName} hatası: {ex.Message}", ex, cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Sub-progress bildirebilen bir startup step'ini yürütür
        /// </summary>
        public static async Task ExecuteStepWithProgressAsync(
            this IStartupApplicationService startupService,
            StartupStep step,
            string stepName,
            Func<IStartupApplicationService, CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            // Step başlat
            await startupService.BeginStepAsync(step, $"{stepName} başlatılıyor...", cancellationToken);

            await Task.Delay(100, cancellationToken);

            try
            {
                // Gerçek işi yap (startupService'ı da geçiyoruz)
                await action(startupService, cancellationToken);

                // Step tamamla
                await startupService.CompleteStepAsync($"{stepName} tamamlandı", cancellationToken);
            } catch(Exception ex)
            {
                await startupService.FailStepAsync($"{stepName} hatası: {ex.Message}", ex, cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Tüm startup sürecini yürütür
        /// </summary>
        public static async Task<bool> ExecuteStartupSequenceAsync(
            this IStartupApplicationService startupService,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ⭐ 2. NAVIGATION CONFIG - BURAYI DOLDURUN
                await startupService.ExecuteStepAsync(
                    StartupStep.NavigationConfiguration,
                    "Navigasyon yapılandırması",
                    () =>
                    {                        
                         Startup.Instance.ConfigureNavigation();
                        return Task.CompletedTask;
                    },
                    cancellationToken);                     
                await ExecuteDatabaseValidationAsync(startupService,cancellationToken);
                await startupService.ExecuteStepAsync(
                    StartupStep.ApplicationUpdateCheck,
                    "Servisler yükleniyor",
                    async () =>
                    {                        
                        await Task.Delay(150, cancellationToken);
                    },
                    cancellationToken);
                await startupService.ExecuteStepAsync(
                    StartupStep.ApplicationUpdateCheck,
                    "Güncelleme kontrolü",
                    async () =>
                    {
                        App.VelopackInitialize();
                        await Task.Delay(150, cancellationToken);
                    },
                    cancellationToken);

                return true;
            } catch(Exception ex)
            {
                await startupService.FailStepAsync($"Başlatma hatası: {ex.Message}", ex, cancellationToken);
                return false;
            }
        }
    }
}