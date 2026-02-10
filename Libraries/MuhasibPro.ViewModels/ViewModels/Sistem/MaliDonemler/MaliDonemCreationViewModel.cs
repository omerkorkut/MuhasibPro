using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.ViewModels.Insrastructure.Common;
using MuhasibPro.ViewModels.Insrastructure.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler
{
    public class MaliDonemCreationViewModel : ViewModelBase
    {
        private readonly ITenantSQLiteDatabaseService _workflowService;
        private TenantCreationResult _creationResult;
        private bool _isCreating;
        private string _currentStepMessage;
        private int _progressPercentage;
        private bool _isCompleted;
        private bool _hasError;

        public MaliDonemCreationViewModel(
            ICommonServices commonServices,
            ITenantSQLiteDatabaseService workflowService)
            : base(commonServices)
        {
            _workflowService = workflowService;
            CreationResult = new TenantCreationResult();
            Steps = new ObservableCollection<CreationStep>();

            // Komutlar
            StartCreationCommand = new RelayCommand<MaliDonemCreationRequest>(async (request) => await StartCreationAsync(request));
            CloseCommand = new RelayCommand(Close);
        }

        // Properties
        public TenantCreationResult CreationResult
        {
            get => _creationResult;
            set => Set(ref _creationResult, value);
        }

        public ObservableCollection<CreationStep> Steps { get; }

        public bool IsCreating
        {
            get => _isCreating;
            set => Set(ref _isCreating, value);
        }

        public string CurrentStepMessage
        {
            get => _currentStepMessage;
            set => Set(ref _currentStepMessage, value);
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => Set(ref _progressPercentage, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => Set(ref _isCompleted, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => Set(ref _hasError, value);
        }

        public bool IsVisible => IsCreating || IsCompleted || HasError;

        // Komutlar
        public ICommand StartCreationCommand { get; }
        public ICommand CloseCommand { get; }

        // Events
        public event EventHandler CreationStarted;
        public event EventHandler<TenantCreationResult> CreationCompleted;
        public event EventHandler CreationCancelled;

        // Ana oluşturma metodu
        public async Task StartCreationAsync(MaliDonemCreationRequest request)
        {
            if (IsCreating)
                return;

            try
            {
                // State'i sıfırla
                ResetState();
                IsCreating = true;
                CreationStarted?.Invoke(this, EventArgs.Empty);

                CurrentStepMessage = "İşlem başlatılıyor...";
                await Task.Delay(300); // UI'nın güncellenmesi için kısa bekleme

                // TenantCreationRequest'e çevir
                var tenantRequest = new TenantCreationRequest
                {
                    FirmaId = request.FirmaId,
                    MaliYil = request.MaliYil,
                    AutoCreateDatabase = true,
                    DatabaseName = request.DatabaseName
                };

                // İşlemi başlat
                await ExecuteWithProgressAsync(
                    async (progressReporter) =>
                    {
                        var response = await _workflowService.CreateNewTenantDatabaseAsync(tenantRequest);

                        if (response.Success && response.Data.DatabaseCreated)
                        {
                            CreationResult = response.Data;

                            // Real-time güncelleme
                            await UpdateUIFromResult(CreationResult);

                            // Progress raporla
                            progressReporter?.Report(CreationResult.ProgressPercentage);
                        }
                        else
                        {
                            HasError=true;
                            IsCompleted = false;
                            throw new Exception(response.Message ?? "İşlem başarısız oldu");
                        }
                    },
                    progressMessage: "Mali dönem oluşturuluyor...",
                    successMessage: "Mali dönem başarıyla oluşturuldu",
                    errorMessage: "Mali dönem oluşturulamadı",
                    measureTime: true,
                    successAutoHideSeconds: 5);
            }
            catch (Exception ex)
            {
                await ContextService.RunAsync(() =>
                {
                    Steps.Clear();
                    CurrentStepMessage = $"Hata: {ex.Message}";
                    ProgressPercentage = 0;
                    HasError=true;
                });
               
                await LogAppExceptionAsync("MaliDonemCreation", "StartCreation", ex);
            }
            finally
            {
                //IsCreating = false;
                await UpdateUIFromResult(CreationResult);
                if (!HasError)
                {
                    IsCompleted = true;
                    CreationCompleted?.Invoke(this, CreationResult);
                }
            }
        }

        // Real-time UI güncellemesi
        private async Task UpdateUIFromResult(TenantCreationResult result)
        {
            // UI thread'inde çalıştır
            await ContextService.RunAsync(() =>
            {
                // Steps koleksiyonunu güncelle
                Steps.Clear();
                foreach (var step in result.Steps)
                {
                    Steps.Add(step);                    
                }

                // Güncel adım mesajı
                if (result.HasError)
                {
                    HasError=true;
                    CurrentStepMessage = $"❌ {result.ErrorMessage}";
                }
                else if (result.CurrentStep != null)
                {
                    var icon = GetStepIcon(result.CurrentStep.Status);
                    CurrentStepMessage = $"{icon} {result.CurrentStep.Message}";
                    
                }
                else if (result.IsSuccess)
                {
                    CurrentStepMessage = $"✅ {result.SuccessMessage}";
                }

                // Progress
                ProgressPercentage = result.ProgressPercentage;
            });
        }

        // Adım icon'ları
        private string GetStepIcon(CreationStepStatus status)
        {
            return status switch
            {
                CreationStepStatus.Bekliyor => "⏳",
                CreationStepStatus.Calisiyor => "🔄",
                CreationStepStatus.Tamamlandi => "✅",
                CreationStepStatus.Hata => "❌",
                CreationStepStatus.Uyari => "⚠️",
                _ => "●"
            };
        }

        // State'i sıfırla
        public void ResetState()
        {
            CurrentStepMessage = string.Empty;
            ProgressPercentage = 0;            
            IsCompleted = false;
            HasError = false;
            Steps.Clear();
            CreationResult = new TenantCreationResult();
        }

        // Panel kapatma
        private void Close()
        {            
            IsCreating = false;
            ResetState();
            CreationCancelled?.Invoke(this, EventArgs.Empty);
        }

        // Otomatik kapanma (başarılı işlemden sonra)
        public async Task AutoCloseAfterSuccessAsync(int delaySeconds = 3)
        {
            if (IsCompleted && !HasError)
            {
                await Task.Delay(delaySeconds * 1000);
                Close();
            }
        }
    }

    // Request model
    public class MaliDonemCreationRequest
    {
        public long FirmaId { get; set; }
        public int MaliYil { get; set; }
        public string DatabaseName { get; set; }
        public string FirmaKodu { get; set; }
        public string FirmaUnvani { get; set; }
    }
}