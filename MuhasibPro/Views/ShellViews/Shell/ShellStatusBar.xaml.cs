using Microsoft.UI.Dispatching;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Converters;
using MuhasibPro.HostBuilders;


namespace MuhasibPro.Views.ShellViews.Shell
{
    public sealed partial class ShellStatusBar : UserControl, IDisposable, INotifyPropertyChanged
    {
        private readonly IStatusMessageService _messageService;
        private readonly IStatusBarService _statusBarService;
        private DispatcherTimer _timer;

        // WRAPPER PROPERTIES - XAML'in anlayacağı dil
        public string StatusMessage => _messageService.StatusMessage;
        public string StatusIconGlyph => _messageService.StatusIconGlyph;
        public string StatusColorHex => _messageService.StatusColorHex;
        public bool ShowStatusIcon => _messageService.ShowStatusIcon;
        public bool IsProgressVisible => _messageService.IsProgressVisible;
        public bool IsProgressIndeterminate => _messageService.IsProgressIndeterminate;
        public double ProgressValue => _messageService.ProgressValue;
        public string ProgressText => _messageService.ProgressText;
        public bool ShowProgressBar => _messageService.ShowProgressBar;

        // StatusBarService properties - direkt
        public string UserName => _statusBarService.UserName;
        public string DatabaseConnectionMessage => _statusBarService.DatabaseConnectionMessage;
        
        public bool IsDatabaseConnection => _statusBarService.IsSistemDatabaseConnection;

        public ShellStatusBar()
        {
            InitializeComponent();

            _messageService = ServiceLocator.Current.GetService<IStatusMessageService>();
            _statusBarService = ServiceLocator.Current.GetService<IStatusBarService>();
            // PropertyChanged event'lerini dinle
            _statusBarService.PropertyChanged += OnServicePropertyChanged;
            _messageService.PropertyChanged += OnServicePropertyChanged;

            this.DataContext = this; // KENDİMİZ!

            InitializeTimer();
        }

        private void OnServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NotifyPropertyChanged(e.PropertyName);

                // Computed property'ler için daha spesifik
                switch (e.PropertyName)
                {
                    case nameof(IStatusMessageService.StatusColorHex):
                        NotifyPropertyChanged(nameof(StatusBrush));
                        break;
                    case nameof(IStatusMessageService.StatusIconGlyph):
                        NotifyPropertyChanged(nameof(StatusGlyph));
                        break;
                    case nameof(IStatusMessageService.IsProgressVisible):
                    case nameof(IStatusMessageService.IsProgressIndeterminate):
                        NotifyPropertyChanged(nameof(ShowProgressBarVisibility));
                        break;
                }
            });
        }

        // Computed properties (XAML için)
        public SolidColorBrush StatusBrush => new SolidColorBrush(ColorConverter.Parse(StatusColorHex));
        public string StatusGlyph => StatusIconGlyph;
        public Visibility ShowProgressBarVisibility => ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowUserInfoVisibility => !string.IsNullOrEmpty(UserName) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowDatabaseInfoVisibility => !string.IsNullOrEmpty(DatabaseConnectionMessage) ? Visibility.Visible : Visibility.Collapsed;
        public SolidColorBrush DatabaseIconBrush => IsDatabaseConnection ?
            new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.OrangeRed);

        private void InitializeTimer()
        {
          
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
            _timer.Start();
        }

        public void Dispose()
        {
            _statusBarService.PropertyChanged -= OnServicePropertyChanged;
            _messageService.PropertyChanged -= OnServicePropertyChanged;
            _timer?.Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}