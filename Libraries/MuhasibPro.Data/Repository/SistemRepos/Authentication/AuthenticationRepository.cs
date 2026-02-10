using Microsoft.AspNetCore.Identity;
using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Exceptions;
using MuhasibPro.Domain.Models;
using MuhasibPro.Domain.Utilities.UIDGenerator;

namespace MuhasibPro.Data.Repository.SistemRepos.Authentication
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<Kullanici> _passwordHasher;

        public AuthenticationRepository(IUserRepository userRepository, IPasswordHasher<Kullanici> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Kullanici> Login(string username, string password)
        {
            Kullanici kullanici = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);

            if (kullanici == null)
            {
                throw new UserNotFoundException(username);
            }
            PasswordVerificationResult passwordResult = _passwordHasher.VerifyHashedPassword(
                kullanici,
                kullanici.ParolaHash,
                password);
            if (passwordResult != PasswordVerificationResult.Success)
            {
                throw new InvalidPasswordException(username, password);
            }
            return kullanici;
        }

        public async Task<RegistrationResult> Register(
            string email,
            string username,
            string password,
            string confirmPassword)
        {
            RegistrationResult result = RegistrationResult.Success;
            if (password != confirmPassword)
            {
                result = RegistrationResult.PasswordsDoNotMatch;
            }
            Kullanici ePosta = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
            if (ePosta != null)
            {
                result |= RegistrationResult.EmailAlreadyExists;
            }
            Kullanici kullaniciAdi = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
            if (ePosta != null)
            {
                result |= RegistrationResult.UsernameAlreadyExists;
            }
            if (result == RegistrationResult.Success)
            {
                string hashedPassword = _passwordHasher.HashPassword(null, password);

                Kullanici kullanici = new Kullanici
                {
                    Id = UIDGenerator.GenerateModuleId(UIDModuleType.Sistem),
                    Eposta = email,
                    KullaniciAdi = username,
                    ParolaHash = hashedPassword,
                    KaydedenId = 241341,
                    KayitTarihi = DateTime.UtcNow,
                    RolId = 1, // Default Role: Admin
                    AktifMi = true,
                };               
                await _userRepository.AddAsync(kullanici).ConfigureAwait(false);
            }
            return result;
        }
    }
}
