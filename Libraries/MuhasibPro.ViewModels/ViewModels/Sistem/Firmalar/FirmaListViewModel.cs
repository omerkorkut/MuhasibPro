using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Business.Services.SistemServices.AppServices;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Enum;
using MuhasibPro.ViewModels.Infrastructure.Common;
using MuhasibPro.ViewModels.Infrastructure.ViewModels;
using System.Linq.Expressions;
using System.Windows.Input;

namespace MuhasibPro.ViewModels.ViewModels.Sistem.Firmalar
{
    public class FirmaListArgs
    {
        public static FirmaListArgs CreateDefault() => new() { IsEmpty = true };

        public bool IsEmpty { get; set; }

        public string Query { get; set; }

        public Expression<Func<Firma, object>> OrderBy { get; set; }

        public Expression<Func<Firma, object>> OrderByDesc { get; set; }

        public Expression<Func<Firma, object>>[] Includes { get; set; }

        public FirmaListArgs()
        {
            OrderBy = f => f.KisaUnvani;
            Includes = new Expression<Func<Firma, object>>[] { f => f.MaliDonemler, };
        }
    }

    public class FirmaListViewModel : GenericListViewModel<FirmaModel>
    {
        private readonly IFirmaService _firmaService;
        public FirmaListViewModel(ICommonServices commonServices, IFirmaService firmaService) : base(commonServices)
        { _firmaService = firmaService; }

        public string Header => "Firma Listesi";

        public FirmaListArgs ViewModelArgs { get; private set; }

        public async Task LoadAsync(FirmaListArgs args)
        {
            ViewModelArgs = args ?? FirmaListArgs.CreateDefault();
            Query = ViewModelArgs.Query;

            await ExecuteActionAsync(
                action: async () => await RefreshAsync(),
                startMessage: $"{Header} yükleniyor",
                startMessageType: StatusMessageType.Refreshing,
                successMessage: $"{Header} yüklendi",
                errorMessage: $"{Header} yükleme hatası");
        }

        public void Unload() { ViewModelArgs.Query = Query; }

        public void Subscribe()
        {
            MessageService.Subscribe<FirmaListViewModel>(this, OnMessage);
            MessageService.Subscribe<FirmaDetailsViewModel>(this, OnMessage);
        }

        public void Unsubscribe() { MessageService.Unsubscribe(this); }
        public FirmaListArgs CreateArgs()
        {
            return new FirmaListArgs
            {
                Query = Query,
                OrderBy = ViewModelArgs.OrderBy,
                OrderByDesc = ViewModelArgs.OrderByDesc,
                Includes = ViewModelArgs.Includes,
            };
        }
        private DataRequest<Firma> BuildDataRequest()
        {
            return new DataRequest<Firma>()
            {
                Query = Query,
                OrderBy = ViewModelArgs.OrderBy,
                OrderByDesc = ViewModelArgs.OrderByDesc,
                Includes = ViewModelArgs.Includes
            };
        }
        public ICommand OpenInNewViewCommand => new RelayCommand(OnOpenInNewView);

        private async void OnOpenInNewView()
        {
            if (SelectedItem != null)
            {
                await NavigationService.CreateNewViewAsync<FirmaDetailsViewModel>(
                    new FirmaDetailsArgs { FirmaId = SelectedItem.Id },
                    customTitle: $"{Header}lar");
            }
        }
        protected override async Task LoadDataAsync()
        {
            if (!ViewModelArgs.IsEmpty)
            {
                DataRequest<Firma> request = BuildDataRequest();
                // TEK servis call - daha hızlı!
                var count = await _firmaService.GetFirmalarCountAsync(request);
                ItemsCount = count.Data; 
                var items = await _firmaService.
                    GetFirmalarPageAsync((CurrentPage - 1) * PageSize, PageSize, request);
                // Items'e ata ve Count'u Items'den al
                Items = items?.Data;
                await ContextService.RunAsync(
                    () =>
                    {
                        // ItemsSource'u Items'den güncelle
                        ItemsSource.Clear();
                        foreach (var item in Items)
                        {
                            ItemsSource.Add(item);
                        }
                        if (!IsMultipleSelection && ItemsSource.Count > 0)
                        {
                            SelectedItem = ItemsSource.FirstOrDefault();
                        }
                    });
            }
            else
            {
                Items = new List<FirmaModel>();      // ← ITEMS'I TEMIZLE
                await ContextService.RunAsync(
                    () =>
                    {
                        ItemsSource?.Clear();
                    });
                ItemsCount = 0;
                SelectedItem = null;
            }
        }

        public async Task<bool> RefreshAsync()
        {
            try
            {
                await LoadDataAsync();
                NotifyPropertyChanged(nameof(Title));  // ✅ SADECE BAŞARILI DURUMDA
                return true;
            } catch(Exception ex)
            {
                StatusError($"{Header} listesi yenilenirken beklenmeyen hata");
                await LogSistemExceptionAsync($"{Header}", "Yenile", ex);
                return false;
            }
        }

        protected async override void OnDeleteSelection()
        {
            StatusReady();
            if (await DialogService.ShowAsync(
                "Silmeyi Onayla",
                $"Seçili {Header}(ları) silmek istediğinize emin misiniz?",
                "Evet",
                "İptal"))
            {
                int count = 0;
                try
                {
                    if (SelectedIndexRanges != null)
                    {
                        count = SelectedIndexRanges.Sum(r => r.Length);
                        StartProgressWithPercent($"{count} Firmalar siliniyor...");

                        await DeleteRangesAsync(SelectedIndexRanges);
                        MessageService.Send(this, "ItemRangesDeleted", SelectedIndexRanges);
                        await RemoveFromUIWithAnimationAsync(SelectedIndexRanges);
                    }
                    else if (SelectedItems != null)
                    {
                        count = SelectedItems.Count();
                        StartProgressWithPercent($"{count} Firmalar siliniyor...");

                        await DeleteItemsAsync(SelectedItems);
                        MessageService.Send(this, "ItemsDeleted", SelectedItems);

                        await RemoveItemsFromUIWithAnimationAsync(SelectedItems);
                    }
                }
                catch (Exception ex)
                {
                    StatusError($"Firma silinirken beklenmeyen hata");
                    await LogSistemExceptionAsync($"{Header}", "Sil", ex);
                    StopProgress();
                    count = 0;
                }
                await RefreshAsync();
                SelectedIndexRanges = null;
                SelectedItems = null;
                StopProgress();
                if (count > 0)
                {
                    StatusActionMessage($"{count} {Header} silindi", StatusMessageType.Deleting, autoHide: -1);
                }
            }
        }
        private async Task DeleteItemsAsync(IEnumerable<FirmaModel> models)
        {
            foreach (var model in models)
            {
                await _firmaService.DeleteFirmaAsync(model.Id);
            }
        }
        private async Task DeleteRangesAsync(IEnumerable<IndexRange> ranges)
        {
            DataRequest<Firma> request = BuildDataRequest();
            foreach (var range in ranges)
            {
                await _firmaService.DeleteFirmaRangeAsync(range.Index, range.Length, request);
            }
        }
        protected override async void OnNew()
        {
            if(IsMainWindow)
                await NavigationService.CreateNewViewAsync<FirmaDetailsViewModel>(
                    new FirmaDetailsArgs(),
                    customTitle: "Yeni Firma");
            else
                NavigationService.Navigate<FirmaDetailsViewModel>(new FirmaDetailsArgs());
            StatusReady();
        }

        protected override async void OnRefresh()
        {
            await ExecuteActionAsync(
                action: async () => await RefreshAsync(),
                startMessage: $"{Header} yenileniyor...",
                startMessageType: StatusMessageType.Refreshing,
                successMessage: $"{Header} yenilendi",
                errorMessage:$"Hata! {Header} alınamadı");
        }

        private async void OnMessage(ViewModelBase sender, string message, object args)
        {
            switch(message)
            {
                case "NewItemSaved":
                case "ItemDeleted":
                case "ItemsDeleted":
                case "ItemRangesDeleted":
                    await ContextService.RunAsync(
                        async () =>
                        {
                            await RefreshAsync();
                        });
                    break;
            }
        }
    }
}
