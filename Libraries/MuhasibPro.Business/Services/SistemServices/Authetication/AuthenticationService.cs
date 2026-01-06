using MuhasibPro.Business.Contracts.CommonServices;
using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.EntityModel;
using MuhasibPro.Business.EntityModel.SistemModel;
using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Models;

namespace MuhasibPro.Business.Services.SistemServices.Authetication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticator _authenticator;
        private readonly IBitmapToolsService _bitmapTools;
        private readonly IMessageService _messageService;
        private HesapModel _currentAccount;


        public AuthenticationService(
            IAuthenticator authenticator,
            IBitmapToolsService bitmapTools,
            IMessageService messageService,
            ModelFactory modelFactory)
        {
            _authenticator = authenticator;
            _bitmapTools = bitmapTools;
            _messageService = messageService;
        }

        public HesapModel CurrentAccount
        {
            get => _currentAccount;
            private set
            {
                if(_currentAccount != value)
                {
                    _currentAccount = value;
                    StateChanged?.Invoke();
                    _messageService.Send(this, "AuthenticationChanged", IsAuthenticated);
                }
            }
        }

        public event Action StateChanged;

        public bool IsAuthenticated => CurrentAccount != null;


        public string CurrentUsername => CurrentAccount?.KullaniciModel.KullaniciAdi ?? "App";

        public long CurrentUserId => CurrentAccount?.Id ?? -1;

        public async Task Login(string username, string password)
        {
            var kullanici = await _authenticator.Login(username, password);
            CurrentAccount = CreateHesapModel(kullanici,false);
        }

        public void Logout()
        {
            _authenticator.Logout();
            CurrentAccount = null;
        }

        public async Task<RegistrationResult> Register(
            string email,
            string username,
            string password,
            string confirmPassword)
        {
            try
            {
                return await _authenticator.Register(email, username, password, confirmPassword).ConfigureAwait(false);
            } catch(Exception ex)
            {
                throw new Exception("Kullanıcı kaydedilmedi", ex);
            }
        }

        private HesapModel CreateHesapModel(Hesap source, bool includeAllFields=false)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));
            try
            {
                var result = ModelFactory.ReadModel<HesapModel, Hesap>(
                    source,
                    includeAllFields,
                    (model, entity, include) =>
                    {
                        model.KullaniciId = entity.KullaniciId;

                        model.SonGirisTarihi = entity.SonGirisTarihi;
                        if(entity.Kullanici != null)
                        {
                            model.KullaniciModel = CreateKullaniciModel(entity.Kullanici, includeAllFields: true);
                        }
                        if(include && entity.FirmaId > 0)
                        {
                            model.FirmaId = entity.FirmaId;
                        }
                    });
                return result;
            } catch(Exception ex)
            {
                // Loglama yapabilirsiniz
                // _logger.LogError(ex, "Hesap modeli oluşturulurken hata");
                throw new Exception($"Hesap oluşturulurken hata oluştu: {ex.Message}", ex);
            }
        }

        private KullaniciModel CreateKullaniciModel(Kullanici source, bool includeAllFields=false)
        {
            try
            {
                var model = ModelFactory.ReadModel<KullaniciModel, Kullanici>(
                    source,
                    includeAllFields,
                    (model, entity, includes) =>
                    {
                        model.Adi = entity.Adi;
                        model.Soyadi = entity.Soyadi;
                        model.Eposta = entity.Eposta;
                        model.KullaniciAdi = entity.KullaniciAdi;
                        model.Telefon = entity.Telefon;
                        model.ResimOnizleme = entity.Resim;
                        model.ResimOnizlemeSource = _bitmapTools.CreateLazyImageLoader(entity.ResimOnizleme); //await _bitmapTools.LoadBitmapAsync(source.ResimOnizleme),
                        model.Resim = entity.Resim;

                        model.ResimSource = entity.Resim;
                        if(entity.Rol != null)
                        {
                            model.Rol = CreateKullaniciRol(entity.Rol);
                            model.RolId = entity.RolId;
                        }
                        if (includes)
                        {
                            model.Resim = source.Resim;
                            model.ResimSource = _bitmapTools.CreateLazyImageLoader(source.Resim);
                        }
                    });
                
                return model;
            } catch(Exception ex)
            {
                throw new Exception("Kullanıcı oluşturulurken hata oluştu", ex);
            }
        }

        private KullaniciRolModel CreateKullaniciRol(KullaniciRol source, bool includesAllFields=false)
        {
            try
            {
                var model = ModelFactory.ReadModel<KullaniciRolModel, KullaniciRol>(
                    source,
                    includesAllFields,
                    (model, entity, include) =>
                    {
                        model.Aciklama = entity.Aciklama;
                        model.RolAdi = entity.RolAdi;
                    });
                return model;
            } catch(Exception)
            {
                throw new Exception("Kullanıcı Rol'ü oluşturulurken hata oluştu");
            }
        }
    }
}
