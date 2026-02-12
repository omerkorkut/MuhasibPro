using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Business.ResultModels.TenantResultModels;
using MuhasibPro.Domain.Enum;
using MuhasibPro.ViewModels.Infrastructure.Common;
using MuhasibPro.ViewModels.Infrastructure.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using static MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler.MaliDonemDeletionViewModel;

namespace MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler
{
    public class MaliDonemDetailsArgs
    {
        public static MaliDonemDetailsArgs CreateDefault() => new MaliDonemDetailsArgs();

        public long FirmaId { get; set; }

        public bool IsNew => MaliDonemId <= 0;

        public long MaliDonemId { get; set; }
    }

    public class MaliDonemDetailsViewModel : GenericDetailsViewModel<MaliDonemModel>
    {
        public MaliDonemDetailsViewModel(
            ICommonServices commonServices,
            IMaliDonemService maliDonemService,
            ITenantSQLiteDatabaseService workflowService) : base(commonServices)
        {
            MaliDonemService = maliDonemService;
            WorkflowService = workflowService;
            CreationViewModel = new MaliDonemCreationViewModel(commonServices, workflowService);
            DeletionViewModel = new MaliDonemDeletionViewModel(commonServices, workflowService);
            CreationViewModel.CreationCompleted += OnCreationCompleted;
            CreationViewModel.CreationCancelled += OnCreationCancelled;
            DeletionViewModel.DeletionCompleted += OnDeletionCompleted;
        }

        private MaliDonemCreationViewModel _creationViewModel;

        public MaliDonemCreationViewModel CreationViewModel
        {
            get => _creationViewModel;
            set => Set(ref _creationViewModel, value);
        }
        private MaliDonemDeletionViewModel _deletionViewModel;
        public MaliDonemDeletionViewModel DeletionViewModel
        {
            get => _deletionViewModel;
            set => Set(ref _deletionViewModel, value);
        }
        private void FirmaSelected(FirmaModel model)
        {
            EditableItem.FirmaId = model.Id;
            EditableItem.FirmaModel = model;
            EditableItem.NotifyChanges();
        }

        private async void OnDetailsMessage(MaliDonemDetailsViewModel sender, string message, MaliDonemModel changed)
        {
            var current = Item;
            if (current != null)
            {
                if (changed != null && changed.Id == current?.Id)
                {
                    switch (message)
                    {
                        case "ItemChanged":
                            await ContextService.RunAsync(
                                async () =>
                                {
                                    try
                                    {
                                        var item = await MaliDonemService.GetByMaliDonemIdAsync(current.Id);
                                        item.Data = item.Data ?? new MaliDonemModel { Id = current.Id, IsEmpty = true };
                                        current.Merge(item.Data);
                                        current.NotifyChanges();
                                        NotifyPropertyChanged(nameof(Title));
                                        if (IsEditMode)
                                        {
                                            StatusActionMessage(
                                                $"DİKKAT: Bu {Header} başkası tarafından değiştirildi!",
                                                StatusMessageType.Warning,
                                                autoHide: 5);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        StatusError($"{Header} bilgileri güncellenirken hata");
                                        await LogSistemExceptionAsync($"{Header}", "Değiştirilmiş", ex);
                                    }
                                });
                            break;
                        case "ItemDeleted":
                            await OnItemDeletedExternally();
                            break;
                    }
                }
            }
        }

        private async Task OnItemDeletedExternally()
        {
            await ContextService.RunAsync(
                () =>
                {
                    CancelEdit();
                    IsEnabled = false;
                    StatusActionMessage($"DİKKAT: Bu {Header} kaydı silinmiş!", StatusMessageType.Warning, autoHide: 5);
                });
        }

        private async void OnListMessage(MaliDonemListViewModel sender, string message, object args)
        {
            var current = Item;
            if (current != null)
            {
                switch (message)
                {
                    case "ItemsDeleted":
                        if (args is IList<MaliDonemModel> deletedModels)
                        {
                            if (deletedModels.Any(r => r.Id == current.Id))
                            {
                                await OnItemDeletedExternally();
                            }
                        }
                        break;
                    case "ItemRangesDeleted":
                        try
                        {
                            var model = await MaliDonemService.GetByMaliDonemIdAsync(current.Id);
                            if (model == null)
                            {
                                await OnItemDeletedExternally();
                            }
                        }
                        catch (Exception ex)
                        {
                            await LogSistemExceptionAsync($"{Header}", $"{Header} kaydı silinmiş!", ex);
                        }
                        break;
                }
            }
        }


        private string Header => "Mali Dönem";

        protected async override Task<bool> ConfirmDeleteAsync()
        {
            return await DialogService.ShowAsync(
                "Silme Onayı",
                $"İlgili {Header}'e ait tüm veriler silinecek! {Header}'i silmek istediğinize emin misiniz?",
                "Sil",
                "İptal");
        }

        protected async override Task<bool> DeleteItemAsync(MaliDonemModel model)
        {
            try
            {
                var request = new MaliDonemDeletionRequest
                {
                    DatabaseName = model.DatabaseName,
                    MaliYil = model.MaliYil,
                    MaliDonemId = model.Id
                };
                await ContextService.RunAsync(
                    async () =>
                    {
                        await DeletionViewModel.StartDeletionAsync(request);
                    });
                if (DeletionViewModel.IsCompleted)
                {                    
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                StatusError($"{Header} silme işleminde beklenmeyen hata");
                await LogSistemExceptionAsync($"{Header}", "Kayıt", ex);
                return false;
            }
        }


        protected async override Task<bool> SaveItemAsync(MaliDonemModel model)
        {
            try
            {
                // Creation request oluştur
                var request = new MaliDonemCreationRequest
                {
                    FirmaId = model.FirmaId,
                    MaliYil = model.MaliYil,
                    FirmaKodu = model.FirmaModel?.FirmaKodu,
                    FirmaUnvani = model.FirmaModel?.KisaUnvani,
                    DatabaseName = model.DatabaseName
                };
                await ContextService.RunAsync(
                    async () =>
                    {
                        // Creation ViewModel'i başlat
                        await CreationViewModel.StartCreationAsync(request);
                    });

                // Creation başarılıysa, modele veritabanı adını set et
                if (CreationViewModel.IsCompleted)
                {
                    model.DatabaseName = CreationViewModel.CreationResult.DatabaseName;
                    model.Id = CreationViewModel.CreationResult.MaliDonemId;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                StatusError($"{Header} kaydedilirken beklenmeyen hata");
                await LogSistemExceptionAsync($"{Header}", "Kayıt", ex);
                return false;
            }
        }


        public MaliDonemDetailsArgs CreateArgs()
        { return new MaliDonemDetailsArgs { FirmaId = Item?.FirmaId ?? 0, MaliDonemId = Item?.Id ?? 0, }; }

        public async Task LoadAsync(MaliDonemDetailsArgs args)
        {
            ViewModelArgs = args ?? MaliDonemDetailsArgs.CreateDefault();
            FirmaId = ViewModelArgs.FirmaId;
            if (ViewModelArgs.IsNew)
            {
                var item = await MaliDonemService.CreateNewMaliDonemForFirmaAsync(FirmaId);
                Item = item?.Data;
                IsEditMode = true;
            }
            else
            {
                try
                {
                    var item = await MaliDonemService.GetByMaliDonemIdAsync(ViewModelArgs.MaliDonemId);
                    Item = item.Data ?? new MaliDonemModel { Id = ViewModelArgs.MaliDonemId, IsEmpty = true };
                }
                catch (Exception ex)
                {
                    StatusError($"{Header} bilgileri yüklenirken beklenmeyen hata");
                    await LogSistemExceptionAsync($"{Header}", $"{Header} Detay", ex);
                }
            }
            NotifyPropertyChanged(nameof(ItemIsNew));
        }

        public void Subscribe()
        {
            MessageService.Subscribe<MaliDonemDetailsViewModel, MaliDonemModel>(this, OnDetailsMessage);
            MessageService.Subscribe<MaliDonemListViewModel>(this, OnListMessage);
        }

        public void Unload()
        {
            ViewModelArgs.FirmaId = Item?.FirmaId ?? 0;
            ViewModelArgs.MaliDonemId = Item?.Id ?? 0;
        }

        public void Unsubscribe() { MessageService.Unsubscribe(this); }

        public bool CanEditFirma => Item?.FirmaId <= 0;

        public long FirmaId { get; set; }

        public ICommand FirmaSelectedCommand => new RelayCommand<FirmaModel>(FirmaSelected);

        public override bool ItemIsNew => Item?.IsNew ?? true;

        public IMaliDonemService MaliDonemService { get; }

        public override string Title => Item?.IsNew ?? true ? "Yeni Mali Dönem" : TitleEdit;

        public string TitleEdit => Item == null ? "Mali Dönem" : $"Mali Dönem #{Item?.MaliYil}";

        public MaliDonemDetailsArgs ViewModelArgs { get; private set; }

        public ITenantSQLiteDatabaseService WorkflowService { get; }

        // Event handlers
        private async void OnCreationCompleted(object sender, TenantCreationResult result)
        {
            // İşlem tamamlandı, UI'ı güncelle
            if (result.IsSuccess)
            {
                // Modeli güncelle
                Item.DatabaseName = result.DatabaseName;
                Item.Id = result.MaliDonemId;
                Item.NotifyChanges();
                StatusActionMessage($"✅ {result.SuccessMessage}", StatusMessageType.Success, autoHide: 5);

                ViewModelArgs = new MaliDonemDetailsArgs { MaliDonemId = result.MaliDonemId, FirmaId = result.FirmaId, };
                
                await LoadAsync(ViewModelArgs);

                // Success mesajı göster
               
                // 3 saniye sonra panel otomatik kapansın
                _ = CreationViewModel.AutoCloseAfterSuccessAsync(3);
            }
        }

        private void OnCreationCancelled(object sender, EventArgs e)
        {
            // Panel kapandı, state'i sıfırla
            CreationViewModel.ResetState();
        }

        private async void OnDeletionCompleted(object sender, TenantDeletingResult result)
        {
            if (result.IsSuccess)
            {                
                StatusActionMessage($"✅ {result.SuccessMessage}", StatusMessageType.Success, autoHide: 5);               
              
                _ = DeletionViewModel.AutoCloseAfterSuccessAsync(3);
                await Task.Delay(3000);                                               
                
                await ContextService.RunAsync(
                    async () =>
                    {                        
                        ViewModelArgs = new MaliDonemDetailsArgs { MaliDonemId = 0, FirmaId = Item?.FirmaId ?? 0, };
                        await LoadAsync(ViewModelArgs);                        
                    });

            }
        }
        private void OnDeletionCancelled(object sender, EventArgs e)
        {
            DeletionViewModel.ResetState();

        }
    }
}
