using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.ViewModels.Insrastructure.ViewModels;

namespace MuhasibPro.ViewModels.ViewModels.Sistem.MaliDonemler
{
    public class MaliDonemViewModel : ViewModelBase
    {
        public MaliDonemViewModel(ICommonServices commonServices, IMaliDonemService maliDonemService, ITenantSQLiteDatabaseService workflowService) : base(commonServices)
        {
            MaliDonemService = maliDonemService;
            //WorkflowService = workflowService;

            MaliDonemList = new MaliDonemListViewModel(commonServices, maliDonemService);
            MaliDonemDetails = new MaliDonemDetailsViewModel(commonServices, maliDonemService,workflowService);
        }
        public IMaliDonemService MaliDonemService { get; }
        //public ITenantWorkflowService WorkflowService { get; }
        public MaliDonemListViewModel MaliDonemList { get; set; }
        public MaliDonemDetailsViewModel MaliDonemDetails { get; set; }
        
        public async Task LoadAsync(MaliDonemDetailsArgs args)
        {
            //await MaliDonemList.LoadAsync(args);
            await MaliDonemDetails.LoadAsync(args);
            long firmaID = args?.FirmaId ?? 0;
            if (firmaID > 0)
            {
                await MaliDonemList.LoadAsync(new MaliDonemListArgs { FirmaId = args.FirmaId }, silent: true);
            }
            else
            {
                await MaliDonemList.LoadAsync(new MaliDonemListArgs { IsEmpty = true }, silent: false);
            }
        }
        public void Unload()
        {
            MaliDonemDetails.CancelEdit();
            MaliDonemList.Unload();
        }

        public void Subscribe()
        {
            MessageService.Subscribe<MaliDonemListViewModel>(this, OnMessage);
            MaliDonemList.Subscribe();
            MaliDonemDetails.Subscribe();
           
        }
        public void Unsubscribe()
        {
            MessageService.Unsubscribe(this);
            MaliDonemList.Unsubscribe();
            MaliDonemDetails.Unsubscribe();
            
        }

        private async void OnMessage(MaliDonemListViewModel viewModel, string message, object args)
        {
            if (viewModel == MaliDonemList && message == "ItemSelected")
            {
                await ContextService.RunAsync(() =>
                {
                    OnItemSelected();
                });
            }
        }
        public async void OnItemSelected()
        {
            if (MaliDonemDetails.IsEditMode)
            {
                StatusReady();
                MaliDonemDetails.CancelEdit();
            }
            var selected = MaliDonemList.SelectedItem;
            if (!MaliDonemList.IsMultipleSelection)
            {
                if (selected != null && !selected.IsEmpty)
                {
                    await PopulateDetails(selected);
                }
            }
            MaliDonemDetails.Item = selected;
        }

        private async Task PopulateDetails(MaliDonemModel selected)
        {
            try
            {
                var model = await MaliDonemService.GetByMaliDonemIdAsync(selected.Id);
                selected.Merge(model.Data);
            }
            catch (Exception ex)
            {
                await LogSistemExceptionAsync("Mali Dönem", "Detaylar", ex);
            }
        }


    }

}
