using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Models;

namespace MuhasibPro.Data.Repository.SistemRepos.Authentication
{
    public class Authenticator : IAuthenticator
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IAccountStore _accountStore;

        public Authenticator(IAuthenticationRepository authenticationRepository, IAccountStore accountStore)
        {
            _authenticationRepository = authenticationRepository;
            _accountStore = accountStore;
        }

        public Hesap CurrentAccount
        {
            get => _accountStore.CurrentAccount;
            private set
            {
                _accountStore.CurrentAccount = value;
                OnStateChanged();
            }
        }

        public bool IsLoggedIn => CurrentAccount != null;

        public event Action StateChanged;

        public async Task<Kullanici> Login(string username, string password)
        {
            var account = await _authenticationRepository.Login(username, password).ConfigureAwait(false);
            if (account != null)
            {
                var hesap = new Hesap();
                hesap.Kullanici = account;
                hesap.KullaniciId = account.Id;
                hesap.SonGirisTarihi=DateTime.UtcNow;
                if(hesap != null)
                    CurrentAccount = hesap;
            }
            return account;
        }

        public void Logout() { CurrentAccount = null; }

        public async Task<RegistrationResult> Register(
            string email,
            string username,
            string password,
            string confirmPassword)
        {
            return await _authenticationRepository.Register(email, username, password, confirmPassword)
                .ConfigureAwait(false);
        }
        protected virtual void OnStateChanged() { Volatile.Read(ref StateChanged)?.Invoke(); }
    }
}
