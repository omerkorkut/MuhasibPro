using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Data.DataContext;

namespace MuhasibPro.Business.Contracts.SistemServices.AppServices
{
    public interface IFirmaWithMaliDonemSelectedService
    {
        FirmaModel SelectedFirma { get; set; }
        MaliDonemModel SelectedMaliDonem { get; set; }
        TenantContext ConnectedTenantDb { get; set; }
        event Action StateChanged;
    }
}
