using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Database.Extensions;
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
        private readonly ITenantSQLiteDatabaseManager _databaseManager;
        private readonly ITenantSQLiteConnectionStringFactory _connectionStringFactory;
        private readonly IMessageService _messageService;
        private readonly ILogService _logService;

        public TenantSQLiteSelectionService(
            ITenantSQLiteSelectionManager selectionManager,
            IMessageService messageService,
            ILogService logService,
            ITenantSQLiteDatabaseManager databaseManager,
            ITenantSQLiteConnectionStringFactory connectionStringFactory)
        {
            _selectionManager = selectionManager;
            _messageService = messageService;
            _logService = logService;

            // Manager'daki değişiklikleri MessageService ile yayınla
            _selectionManager.TenantChanged += OnManagerTenantChanged;
            _databaseManager = databaseManager;
            _connectionStringFactory = connectionStringFactory;
        }

        private void OnManagerTenantChanged(TenantContext tenant)
        {
            // Manager'daki değişikliği MessageService ile UI'ya ilet
            _messageService.Send(this, "TenantChanged", tenant);
        }

        public bool IsTenantLoaded => _selectionManager.IsTenantLoaded;

        public void ClearCurrentTenantAsync()
            => _selectionManager.ClearCurrentTenant();

        public async Task<ApiDataResponse<bool>> DisconnectCurrentTenantAsync()
        {
            try
            {
                if (!IsTenantLoaded)
                {
                    return new ErrorApiDataResponse<bool>(
                        data: false,
                        message: "🟢 Zaten aktif bir bağlantı bulunamadı");
                }

                ClearCurrentTenantAsync();

                await _logService.SistemLogService.SistemLogInformationAsync(
                    "Mali Dönem Seçimi",
                    "Veritabanı İşlemleri",
                    "Seçili veritabanı bağlantısı kesildi",
                    "Veritabanı bağlantısı kullanıcı tarafından kesildi");

                // Kısa bekle ve kontrol et
                await Task.Delay(100);

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
            string databaseName)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return new ErrorApiDataResponse<TenantContext>(
                    data: null,
                    message: "Veritabanı adı boş olamaz");
            }

            // Aynı tenant kontrolü (case-insensitive)
            if (IsTenantLoaded &&
                string.Equals(CurrentTenant.DatabaseName, databaseName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorApiDataResponse<TenantContext>(
                    data: CurrentTenant,
                    message: "Zaten bu mali dönemi kullanıyorsunuz!");
            }
            var validateConnection = await _connectionStringFactory.ValidateConnectionStringAsync(databaseName);
            if(!validateConnection.canConnect)
            {
                return new ErrorApiDataResponse<TenantContext>(
                    data: null,
                    message: $"Veritabanı bağlantı dizesi oluşturulamadı: {databaseName}");
            }
            var tenantContext = new TenantContext
            {
                DatabaseName = databaseName,
                DatabaseType = Domain.Enum.DatabaseEnum.DatabaseType.SQLite,
                ConnectionString = validateConnection.connectionString,
                 LoadedAt = DateTime.UtcNow,
                 Message = validateConnection.canConnect ? null : $"Veritabanına bağlantı doğrulanamadı: {databaseName}"

            };
            var initilizeDatabase = await _databaseManager.InitializeTenantDatabaseAsync(databaseName);
            if(!initilizeDatabase.IsHealthy)
            {
                return ApiDataExtensions.ErrorResponse<TenantContext>(CurrentTenant, initilizeDatabase.ToUIFullMessage());
            }
            try
            {
                var newTenant = _selectionManager.SwitchToTenantAsync(
                    tenantContext);

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