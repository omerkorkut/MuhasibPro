using MuhasibPro.Business.Contracts.SistemServices.AppServices;
using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Data.DataContext;

namespace MuhasibPro.Business.Services.SistemServices.AppServices
{
    public class FirmaWithMaliDonemSelectedService : IFirmaWithMaliDonemSelectedService
    {
        private FirmaModel _selectedFirma;
        private MaliDonemModel _selectedMaliDonem;
        private TenantContext _connectedTenanDb;
        public FirmaWithMaliDonemSelectedService()
        {
        }

        public FirmaModel SelectedFirma
        {
            get => _selectedFirma;
            set
            {
                if (_selectedFirma != value)
                {
                    _selectedFirma = value;
                    StateChanged?.Invoke();                    
                }
            }
        }

        public MaliDonemModel SelectedMaliDonem
        {
            get => _selectedMaliDonem;
            set
            {
                if (_selectedMaliDonem != value)
                {
                    _selectedMaliDonem = value;
                    StateChanged?.Invoke();
                }
            }
        }

        public TenantContext ConnectedTenantDb
        {
            get => _connectedTenanDb;
            set
            {
                if (_connectedTenanDb != value)
                {
                    _connectedTenanDb = value;
                    StateChanged?.Invoke();
                }
            }
        }
        public event Action StateChanged;

    }
}
