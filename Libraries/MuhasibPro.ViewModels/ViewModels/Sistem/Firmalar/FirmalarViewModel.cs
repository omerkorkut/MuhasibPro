using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.ViewModels.Insrastructure.ViewModels;
using MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler;

namespace MuhasibPro.ViewModels.ViewModels.Sistem.Firmalar;
public class FirmalarViewModel : ViewModelBase
{
    public FirmalarViewModel(ICommonServices commonServices, IFirmaService firmaService, IFilePickerService filePickerService, IMaliDonemService maliDonemService) : base(commonServices)
    {
        FirmaService = firmaService;
        MaliDonemService = maliDonemService;

        FirmaList = new FirmaListViewModel(commonServices, FirmaService);
        FirmaDetails = new FirmaDetailsViewModel(commonServices, filePickerService, FirmaService);
        MaliDonemList = new MaliDonemListViewModel(commonServices,MaliDonemService);
        
    }
    public IFirmaService FirmaService { get; }
    public IMaliDonemService MaliDonemService { get; }
    public FirmaListViewModel FirmaList { get; set; }
    public FirmaDetailsViewModel FirmaDetails { get; set; }
    public MaliDonemListViewModel MaliDonemList { get; set; }
    

    private string Header = "Firma";
    public async Task LoadAsync(FirmaListArgs args)
    {
        await FirmaList.LoadAsync(args);
    }
    public void Unload()
    {
        FirmaDetails.CancelEdit();
        FirmaList.Unload();
    }
    public void Subscribe()
    {
        MessageService.Subscribe<FirmaListViewModel>(this, OnMessage);
        FirmaList.Subscribe();
        FirmaDetails.Subscribe();
        MaliDonemList.Subscribe();
    }
    public void Unsubscribe()
    {
        MessageService.Unsubscribe(this);
        FirmaList.Unsubscribe();
        FirmaDetails.Unsubscribe();
        MaliDonemList.Unsubscribe();

    }
    private async void OnMessage(FirmaListViewModel viewModel, string message, object args)
    {
        if (viewModel == FirmaList && message == "ItemSelected")
        {
            await ContextService.RunAsync(() =>
            {
                OnItemSelected();
            });
        }
    }

    public async void OnItemSelected()
    {
        if (FirmaDetails.IsEditMode)
        {
            StatusReady();
            FirmaDetails.CancelEdit();
        }
        //CustomerOrders.IsMultipleSelection = false;
        var selected = FirmaList.SelectedItem;
        if (!FirmaList.IsMultipleSelection)
        {
            if (selected != null && !selected.IsEmpty)
            {
                await PopulateDetails(selected);
                await PopulateMaliDonem(selected);
            }
        }
        FirmaDetails.Item = selected;
    }

    private async Task PopulateDetails(FirmaModel selected)
    {
        try
        {
            var model = await FirmaService.GetByFirmaIdAsync(selected.Id);
            selected.Merge(model.Data);
        }
        catch (Exception ex)
        {
            await LogSistemExceptionAsync($"{Header}lar", $"{Header} Detay", ex);
        }
    }
    private async Task PopulateMaliDonem(FirmaModel selectedItem)
    {
        try
        {
            if(selectedItem != null)
            {
                await MaliDonemList.LoadAsync(new MaliDonemListArgs { FirmaId = selectedItem.Id },silent:true);
            }
        }
        catch (Exception ex)
        {
            await LogSistemExceptionAsync("Firmalar", "Mali Dönemler", ex);
        }
    }
}
