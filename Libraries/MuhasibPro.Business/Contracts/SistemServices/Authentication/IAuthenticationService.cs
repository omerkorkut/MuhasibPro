using MuhasibPro.Business.DTOModel.SistemModel;
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
        string GetCurrentUsername { get; }
        long GetCurrentUserId { get; }
        event Action StateChanged;
    }
}
