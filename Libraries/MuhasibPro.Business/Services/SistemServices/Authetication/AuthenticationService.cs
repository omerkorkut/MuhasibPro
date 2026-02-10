using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.Contracts.UIServices.CommonServices;
using MuhasibPro.Business.DTOModel;
using MuhasibPro.Business.DTOModel.SistemModel;
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


        public string GetCurrentUsername => CurrentAccount?.KullaniciModel.KullaniciAdi ?? "App";

        public long GetCurrentUserId => CurrentAccount?.KullaniciId ?? -1;

        public async Task Login(string username, string password)
        {
            var kullanici = await _authenticator.Login(username, password);
            var hesapModel = new HesapModel();
            if (kullanici != null) 
            {
                hesapModel.KullaniciModel = CreateKullaniciModel(kullanici,true);
                hesapModel.KullaniciId = kullanici.Id;
                hesapModel.SonGirisTarihi = DateTime.UtcNow;
                CurrentAccount = hesapModel;
            }
            
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

        private KullaniciModel CreateKullaniciModel(Kullanici source, bool includeAllFields=false)
        {
            try
            {
                var model = ModelFactory.CreateModelFromEntity<KullaniciModel, Kullanici>(
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
                var model = ModelFactory.CreateModelFromEntity<KullaniciRolModel, KullaniciRol>(
                    source,
                    includesAllFields,
                    (model, entity, include) =>
                    {
                        model.Aciklama = entity.Aciklama;
                        model.RolAdi = entity.RolAdi;
                        model.RolTip = entity.RolTip;
                    });
                return model;
            } catch(Exception)
            {
                throw new Exception("Kullanıcı Rol'ü oluşturulurken hata oluştu");
            }
        }
    }
}
