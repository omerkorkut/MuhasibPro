using MuhasibPro.Business.Contracts.CommonServices;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    /// <summary>
    /// public class MainViewModel : ObservableObject
    ///{
    ///    public MainViewModel(IMessageService messageService)
    ///    {
    ///        // Tenant değişikliklerini dinle
    ///        messageService.Subscribe<TenantContext>(
    ///            this,
    ///            "TenantChanged",
    ///            OnTenantChanged);
    ///    }

    ///    private void OnTenantChanged(TenantContext tenant)
    ///    {
    ///        // UI thread'de çalıştır (WinUI3)
    ///        _ = DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
    ///        {
    ///            CurrentDatabase = tenant.DatabaseName;
    ///            IsConnected = tenant.IsLoaded;
    ///
    ///            /// İsteğe bağlı: Mesaj göster
    ///            if (!tenant.IsLoaded && !string.IsNullOrEmpty(tenant.Message))
    ///            {
    ///                ShowToast(tenant.Message);
    ///            }
    ///        });
    ///    }

    ///    [ObservableProperty]
    ///    private string _currentDatabase;

    ///    [ObservableProperty]
    ///    private bool _isConnected;
    ///}
    /// </summary>
    public class TenantSQLiteSelectionService : ITenantSQLiteSelectionService
    {
        private readonly ITenantSQLiteSelectionManager _selectionManager;
        private readonly IMessageService _messageService;
        private readonly ILogService _logService;

        public TenantSQLiteSelectionService(
            ITenantSQLiteSelectionManager selectionManager,
            IMessageService messageService,
            ILogService logService)
        {
            _selectionManager = selectionManager;
            _messageService = messageService;
            _logService = logService;

            // Manager'daki değişiklikleri MessageService ile yayınla
            _selectionManager.TenantChanged += OnManagerTenantChanged;
        }

        private void OnManagerTenantChanged(TenantContext tenant)
        {
            // Manager'daki değişikliği MessageService ile UI'ya ilet
            _messageService.Send(this, "TenantChanged", tenant);
        }

        public bool IsTenantLoaded => _selectionManager.IsTenantLoaded;

        public async Task ClearCurrentTenantAsync(CancellationToken cancellationToken = default)
            => await _selectionManager.ClearCurrentTenantAsync(cancellationToken);

        public async Task<ApiDataResponse<bool>> DisconnectCurrentTenantAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsTenantLoaded)
                {
                    return new ErrorApiDataResponse<bool>(
                        data: false,
                        message: "🟢 Zaten aktif bir bağlantı bulunamadı");
                }

                await ClearCurrentTenantAsync(cancellationToken);

                await _logService.SistemLogService.SistemLogInformationAsync(
                    "Mali Dönem Seçimi",
                    "Veritabanı İşlemleri",
                    "Seçili veritabanı bağlantısı kesildi",
                    "Veritabanı bağlantısı kullanıcı tarafından kesildi");

                // Kısa bekle ve kontrol et
                await Task.Delay(100, cancellationToken);

                if (IsTenantLoaded)
                {
                    return new ErrorApiDataResponse<bool>(
                        data: false,
                        message: "⚠️ [UYARI] Veritabanı bağlantısı kesilemedi");
                }

                return new SuccessApiDataResponse<bool>(
                    data: true,
                    message: "⛓️‍💥 Aktif veritabanı bağlantısı başarıyla kesildi");
            }
            catch (Exception ex)
            {
                await _logService.SistemLogService.SistemLogExceptionAsync(
                    "Mali Dönem Seçimi", "Veritabanı İşlemleri", ex);
                return new ErrorApiDataResponse<bool>(
                    false,
                    message: $"[HATA] Bağlantı kesilemedi : {ex.Message}");
            }
        }

        public TenantContext CurrentTenant => _selectionManager.GetCurrentTenant();

        public async Task<ApiDataResponse<TenantContext>> SwitchTenantAsync(
            string databaseName,
            CancellationToken cancellationToken)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return new ErrorApiDataResponse<TenantContext>(
                    data: null,
                    message: "📛 Veritabanı adı boş olamaz");
            }

            // Aynı tenant kontrolü (case-insensitive)
            if (IsTenantLoaded &&
                string.Equals(CurrentTenant.DatabaseName, databaseName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorApiDataResponse<TenantContext>(
                    data: CurrentTenant,
                    message: "🔁 Zaten bu mali dönemi kullanıyorsunuz!");
            }

            try
            {
                var newTenant = await _selectionManager.SwitchToTenantAsync(
                    databaseName, cancellationToken);

                // Business logging - sadece başarılıysa
                if (newTenant.IsLoaded)
                {
                    await _logService.SistemLogService.SistemLogInformationAsync(
                        "Mali Dönem Seçimi",
                        "Veritabanı İşlemleri",
                        "Seçili dönem değiştirildi.",
                        $"Yeni dönem: {databaseName}");
                }

                // Response mesajı
                var responseMessage = newTenant.IsLoaded
                    ? $"✅ {databaseName} dönemine geçildi"
                    : newTenant.Message ?? "❌ Bağlantı kurulamadı";

                return newTenant.IsLoaded
                    ? new SuccessApiDataResponse<TenantContext>(newTenant, responseMessage)
                    : new ErrorApiDataResponse<TenantContext>(newTenant, responseMessage);
            }
            catch (Exception ex)
            {
                await _logService.SistemLogService
                   .SistemLogExceptionAsync("Mali Dönem Seçimi",
                       $"Veritabanı İşlemleri - {databaseName}", ex);
                return new ErrorApiDataResponse<TenantContext>(
                    data: null,
                    message: $"[HATA] İşlem başarısız: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _selectionManager.TenantChanged -= OnManagerTenantChanged;
        }
    }
}