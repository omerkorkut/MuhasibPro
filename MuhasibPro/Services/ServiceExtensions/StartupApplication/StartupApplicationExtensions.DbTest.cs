using MuhasibPro.Configurations;
using MuhasibPro.Contracts.UIService;

namespace MuhasibPro.Services.ServiceExtensions.StartupApplication
{
    public static partial class StartupApplicationExtensions
    {
        public static async Task ExecuteDatabaseValidationAsync(
            this IStartupApplicationService startupService,
            CancellationToken cancellationToken = default)
        {
            await startupService.ExecuteStepWithProgressAsync(
                StartupStep.DatabaseValidation,
                "Veritabanı doğrulama",
                async (service, ct) =>
                {
                    // 1. Başlangıç
                    await service.ReportSubProgressAsync("Veritabanı bağlantısı kontrol ediliyor...", 10, ct);

                    // 2. DB Test
                    var dbResult = await Startup.Instance.InitializeSistemDatabase();

                    // 3. Sonuç
                    if (dbResult.isValid)
                    {
                        await service.ReportSubProgressAsync($"{dbResult.message}", 50, ct);
                        await Task.Delay(300, ct);

                        await service.ReportSubProgressAsync("Veritabanı hazırlanıyor...", 80, ct);
                        await Task.Delay(200, ct);
                    }
                    else
                    {                        
                        throw new InvalidOperationException($"❌ {dbResult.message}");
                    }
                },
                cancellationToken);
        }
    }
}
