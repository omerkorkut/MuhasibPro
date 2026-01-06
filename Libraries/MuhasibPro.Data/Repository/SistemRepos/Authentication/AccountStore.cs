using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Repository.SistemRepos.Authentication
{
    public class AccountStore : IAccountStore
    {
        private Hesap _currentAccount;

        public Hesap CurrentAccount
        {
            get { return _currentAccount; }
            set
            {
                _currentAccount = value;
                StateChanged?.Invoke();
            }
        }

        public event Action StateChanged;
    }
}
