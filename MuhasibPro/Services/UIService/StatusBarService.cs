using Microsoft.UI.Dispatching;
using MuhasibPro.Business.Contracts.UIServices;

namespace MuhasibPro.Services.UIService
{
    public class StatusBarService : INotifyPropertyChanged, IStatusBarService
    {
        private DispatcherQueue _dispatcherQueue = null;

        private string _userName;
        private string _kullaniciAdiSoyadi;
        private string _databaseConnectionMessage;
        private bool _isSistemDatabaseConnection;
        private bool _isTenantDatabaseConnection;
        private int _maliDonem;
        public StatusBarService()
        {

        }

        public void Initialize(object dispatcher)
        {
            _dispatcherQueue = dispatcher as DispatcherQueue;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Public Properties
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    NotifyPropertyChanged(nameof(UserName));
                }
            }
        }
        public string KullaniciAdiSoyadi
        {
            get => _kullaniciAdiSoyadi;
            set
            {
                if (_kullaniciAdiSoyadi != value)
                {
                    _kullaniciAdiSoyadi = value;
                    NotifyPropertyChanged(nameof(KullaniciAdiSoyadi));
                }
            }
        }
        public int MaliDonem
        {
            get => _maliDonem;
            set
            {
                if (_maliDonem != value)
                {
                    _maliDonem = value;
                    NotifyPropertyChanged(nameof(MaliDonem));
                }
            }
        }

        public string DatabaseConnectionMessage
        {
            get => _databaseConnectionMessage;
            set
            {
                if (_databaseConnectionMessage != value)
                {
                    _databaseConnectionMessage = value;
                    NotifyPropertyChanged(nameof(DatabaseConnectionMessage));
                }
            }
        }

        public bool IsSistemDatabaseConnection
        {
            get => _isSistemDatabaseConnection;
            set
            {
                if (_isSistemDatabaseConnection != value)
                {
                    _isSistemDatabaseConnection = value;
                    NotifyPropertyChanged(nameof(IsSistemDatabaseConnection));
                }
            }
        }
        public bool IsTenantDatabaseConnection
        {
            get => _isTenantDatabaseConnection;
            set
            {
                if (_isTenantDatabaseConnection != value)
                {
                    _isTenantDatabaseConnection = value;
                    NotifyPropertyChanged(nameof(IsTenantDatabaseConnection));
                }
            }
        }

        public void SetSistemDatabaseStatus(bool isConnected, string message = null)
        {
            ExecuteOnUIThread(
                () =>
                {
                    IsSistemDatabaseConnection = isConnected;
                    if (!string.IsNullOrEmpty(message))
                        DatabaseConnectionMessage = message;
                });
        }
        public void SetTenantDatabaseStatus(bool isConnected, string message = null)
        {
            ExecuteOnUIThread(
                () =>
                {
                    IsTenantDatabaseConnection = isConnected;
                    if (!string.IsNullOrEmpty(message))
                        DatabaseConnectionMessage = message;
                });
        }
        #endregion

        #region Private Methods
        private void ExecuteOnUIThread(Action action)
        {
            if (_dispatcherQueue != null)
                _dispatcherQueue.TryEnqueue(() => action());
            else
                action();
        }

        public void NotifyPropertyChanged(string propertyName)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        #endregion
    }
}