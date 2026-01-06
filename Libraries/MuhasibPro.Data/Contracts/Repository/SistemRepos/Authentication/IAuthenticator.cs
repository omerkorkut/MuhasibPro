using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Models;

namespace MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication
{
    public interface IAuthenticator
    {
        Hesap CurrentAccount { get; }
        bool IsLoggedIn { get; }

        event Action StateChanged;
        void Logout();
        Task<Hesap> Login(string username, string password);
        Task<RegistrationResult> Register(
            string email,
            string username,
            string password,
            string confirmPassword);
    }
}
