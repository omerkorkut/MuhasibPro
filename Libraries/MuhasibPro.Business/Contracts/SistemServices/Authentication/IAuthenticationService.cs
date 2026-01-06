using MuhasibPro.Business.EntityModel.SistemModel;
using MuhasibPro.Domain.Models;

namespace MuhasibPro.Business.Contracts.SistemServices.Authentication
{
    public interface IAuthenticationService
    {
        HesapModel CurrentAccount { get; }
        bool IsAuthenticated { get; }
        Task<RegistrationResult> Register(string email, string username, string password, string confirmPassword);
        Task Login(string username, string password);
        void Logout();
        string CurrentUsername { get; }
        long CurrentUserId { get; }
        event Action StateChanged;
    }
}
