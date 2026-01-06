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
        private readonly IPasswordHasher<Hesap> _passwordHasher;

        public AuthenticationRepository(IUserRepository userRepository, IPasswordHasher<Hesap> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Hesap> Login(string username, string password)
        {
            Hesap kullanici = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
            if (kullanici == null)
            {
                throw new UserNotFoundException(username);
            }
            PasswordVerificationResult passwordResult = _passwordHasher.VerifyHashedPassword(
                kullanici,
                kullanici.Kullanici.ParolaHash,
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
            Hesap ePosta = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
            if (ePosta != null)
            {
                result |= RegistrationResult.EmailAlreadyExists;
            }
            Hesap kullaniciAdi = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
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
                Hesap hesap = new Hesap
                {
                    Id = UIDGenerator.GenerateModuleId(UIDModuleType.Sistem),
                    KayitTarihi = DateTime.UtcNow,
                    AktifMi = true,
                    Kullanici = kullanici,
                    KullaniciId = kullanici.Id,
                    SonGirisTarihi = DateTime.UtcNow,
                    KaydedenId = kullanici.KaydedenId,
                };
                await _userRepository.AddAsync(hesap).ConfigureAwait(false);
            }
            return result;
        }
    }
}
