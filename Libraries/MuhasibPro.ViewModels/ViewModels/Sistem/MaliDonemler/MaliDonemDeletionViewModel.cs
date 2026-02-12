using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.ViewModels.Infrastructure.Common;
using MuhasibPro.ViewModels.Infrastructure.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler
{
    public class MaliDonemDeletionViewModel : ViewModelBase
    {
        private readonly ITenantSQLiteDatabaseService _tenantDatabaseService;
        private TenantDeletingResult _deletionResult;
        private bool _isDeleting;
        private string _currentStepMessage;
        private int _progressPercentage;
        private bool _isCompleted;
        private bool _hasError;
        public MaliDonemDeletionViewModel(ICommonServices commonServices, ITenantSQLiteDatabaseService tenantDatabaseService) : base(commonServices)
        {
            DeletionResult = new TenantDeletingResult();
            _tenantDatabaseService = tenantDatabaseService;
            Steps = new ObservableCollection<DeletionStep>();

            StartDeletionCommand = new RelayCommand<MaliDonemDeletionRequest>(async (request) => await StartDeletionAsync(request));
            CloseCommand = new RelayCommand(Close);
        }
        public TenantDeletingResult DeletionResult
        {
            get => _deletionResult;
            set => Set(ref _deletionResult, value);
        }
        public ObservableCollection<DeletionStep> Steps { get; }
        public bool IsDeleting
        {
            get => _isDeleting;
            set => Set(ref _isDeleting, value);
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
        public bool IsVisible => IsDeleting || IsCompleted || HasError;
        public ICommand StartDeletionCommand { get; }
        public ICommand CloseCommand { get; }

        //eventler
        public event EventHandler DeletionStarted;
        public event EventHandler<TenantDeletingResult> DeletionCompleted;
        public event EventHandler DeletionCancelled;

        public async Task StartDeletionAsync(MaliDonemDeletionRequest request)
        {
            if (IsDeleting)
                return;
            try
            {
                ResetState();
                IsDeleting = true;
                DeletionStarted?.Invoke(this, EventArgs.Empty);

                CurrentStepMessage = "Silme işlemi başlatılıyor...";
                await Task.Delay(300); // Kullanıcıya mesajın görünmesi için kısa bir gecikme
                
                var tenantReqest = new TenantDeletingRequest
                {
                    MaliDonemId = request.MaliDonemId,
                    DatabaseName = request.DatabaseName,
                    IsDeleteMaliDonem = true,
                    IsDeleteDatabase = true,
                    DeleteAllTenantBackup = true,
                    IsCurrentTenantDeletingBeforeBackup = false,                    
                };
                await ExecuteWithProgressAsync(
                    async (progressReporter) =>
                {
                    var response = await _tenantDatabaseService.DeleteTenantDatabaseAsync(tenantReqest);
                    if (response.Success && response.Data.DeleteCompleted)
                    {
                        DeletionResult = response.Data;
                        await UpdateUIFromResult(DeletionResult);

                        progressReporter?.Report(DeletionResult.ProgressPercentage);
                        
                    }
                    else 
                    {
                        HasError= true;
                        IsCompleted= false;
                        throw new Exception(response.Message ?? "İşlem başarısız oldu");
                    }
                },
                progressMessage:"Mali dönem siliniyor",
                successMessage:"Mali dönem başarıyla silindi",
                errorMessage:"Mali dönem silinemedi",
                measureTime:true,
                successAutoHideSeconds:5);
            }
            catch (Exception ex)
            {
                await ContextService.RunAsync(() =>
                {
                    Steps.Clear();
                    CurrentStepMessage = $"Hata: {ex.Message}";
                    ProgressPercentage = 0;
                    HasError = true;
                });
                
                await LogAppExceptionAsync("MaliDonemDeletion", "StartDeletion", ex);
            }
            finally
            {
                //IsCreating = false;
                await UpdateUIFromResult(DeletionResult);
                if (!HasError)
                {
                    IsCompleted = true;
                    DeletionCompleted?.Invoke(this, DeletionResult);
                }
            }
        }
        private async Task UpdateUIFromResult(TenantDeletingResult result)
        {
            await ContextService.RunAsync(() =>
            {
                Steps.Clear();
                foreach (var step in result.Steps)
                {
                    Steps.Add(step); // Adımın başlatıldığını simüle et
                    
                }
                if(result.HasError)
                {
                    HasError = true;
                    CurrentStepMessage = $"Hata: {result.ErrorMessage}";
                }
                else if(result.CurrentStep != null)
                {
                    var icon = GetStepIcon(result.CurrentStep.Status);
                    CurrentStepMessage = $"{icon} {result.CurrentStep.Message}";
                    
                }
                else if (result.IsSuccess)
                {
                    IsCompleted = true;
                    CurrentStepMessage = $"✅ {result.SuccessMessage}";
                }
                // Progress
                ProgressPercentage = result.ProgressPercentage;
            });
        }
        private string GetStepIcon(DeletionStepStatus status)
        {
            return status switch
            {
                DeletionStepStatus.Bekliyor => "⏳",
                DeletionStepStatus.Calisiyor => "🔄",
                DeletionStepStatus.Tamamlandi => "✅",
                DeletionStepStatus.Hata => "❌",
                DeletionStepStatus.Uyari => "⚠️",
                _ => "●"
            };
        }
        public void ResetState()
        {           
            CurrentStepMessage = string.Empty;
            ProgressPercentage = 0;
            IsCompleted = false;
            HasError = false;
            Steps.Clear();
            DeletionResult = new TenantDeletingResult();
        }
        private void Close()
        {
            IsDeleting = false;
            ResetState();
            DeletionCancelled?.Invoke(this, EventArgs.Empty);

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
        public class MaliDonemDeletionRequest
        {
            public long MaliDonemId { get; set; }            
            public int MaliYil { get; set; }
            public string DatabaseName { get; set; }            
            
        }
    }
}
