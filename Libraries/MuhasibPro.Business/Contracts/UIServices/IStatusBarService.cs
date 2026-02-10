using System.ComponentModel;

namespace MuhasibPro.Business.Contracts.UIServices
{
    /// <summary>
    /// StatusBar üst bilgileri yönetimi (User, Database, Save status)
    /// Mesaj işlemleri için IStatusMessageService kullanın
    /// </summary>
    public interface IStatusBarService : INotifyPropertyChanged
    {
        #region Properties
        /// <summary>
        /// Kullanıcı adı (StatusBar'da gösterilir)
        /// </summary>
        string UserName { get; set; }
        string KullaniciAdiSoyadi { get; set; }
        int MaliDonem {  get; set; }

        /// <summary>
        /// Veritabanı bağlantı mesajı
        /// </summary>
        string DatabaseConnectionMessage { get; set; }

        /// <summary>
        /// Veritabanı bağlantı durumu (true: bağlı, false: bağlı değil)
        /// </summary>
        bool IsSistemDatabaseConnection { get; set; }
        bool IsTenantDatabaseConnection { get; set; }


        #endregion

        #region Methods
        /// <summary>
        /// Dispatcher'ı başlat (UI thread için)
        /// </summary>
        void Initialize(object dispatcher);

        /// <summary>
        /// Kayıt durumunu ayarla
        /// </summary>


        /// <summary>
        /// Veritabanı durumunu ayarla
        /// </summary>
        void SetSistemDatabaseStatus(bool isConnected, string message = null);
        void SetTenantDatabaseStatus(bool isConnected, string message = null);
        #endregion
    }
}