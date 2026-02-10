// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Configurations;
using MuhasibPro.Contracts.UIService;
using MuhasibPro.Domain.Helpers;
using MuhasibPro.Extensions;
using MuhasibPro.Helpers.WindowHelpers;
using MuhasibPro.HostBuilders;
using MuhasibPro.Services.UIService;
using MuhasibPro.ViewModels.ViewModels.Shell;
using MuhasibPro.Views.Login;
using System.Diagnostics;

namespace MuhasibPro.Views.ShellViews.Splash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExtendedSplash : Page, INotifyPropertyChanged
    {
        private readonly Frame rootFrame;
        private bool _isProcessingComplete = false;
        private string _currentMessageText = "";
        private readonly IStartupApplicationService _startupService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ExtendedSplash()
        {
            InitializeComponent();
            rootFrame = new Frame();
            // BU 3 SATIRI EKLEYÝN:
            _startupService = ServiceLocator.Current.GetService<IStartupApplicationService>();
            _startupService.ProgressChanged += OnStartupProgressChanged;
            Unloaded += OnPageUnloaded;          
        }

        public static Queue<string> StatusMessages { get; } = new Queue<string>();

        public string Version => ProcessInfoHelper.Version;

        public string CopyrightText
        {
            get
            {
                int currentYear = DateTime.Now.Year;
                return $"© {currentYear} {ProcessInfoHelper.ProductName}";
            }
        }

        public string ApplicationName { get { return $"{ProcessInfoHelper.ProductName}"; } }

        private double _progressValue;

        public double ProgressValue
        {
            get => _progressValue;
            private set
            {
                if(Math.Abs(_progressValue - value) > 0.01)
                {
                    _progressValue = Math.Clamp(value, 0, 100);
                    NotifyPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            // BU METHODU EKLEYÝN:
            if(_startupService != null)
            {
                _startupService.ProgressChanged -= OnStartupProgressChanged;
            }
            Unloaded -= OnPageUnloaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            FadeInStoryboard.Begin();

           
            var success = await _startupService.InitializeAsync();

            if(success)
            {
                await MarkProcessingCompleteAsync();
            } else
            {
                await ShowNotificationAsync("Uygulama baþlatýlamadý", "Hata");
                await Task.Delay(3000);
                Application.Current.Exit();
            }
        }

        private void OnStartupProgressChanged(object sender, StartupProgressEventArgs e)
        {
            // BU METHODU EKLEYÝN:
            DispatcherQueue.TryEnqueue(
                async () =>
                {
                    await ShowStatusWithTransitionAsync(e.Message);
                    ProgressValue = e.Progress;
                });
        }


        private void NotifyPropertyChanged(string propertyName)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        private async Task ShowStatusWithTransitionAsync(string statusText)
        {
            try
            {
                if(CurrentMessage == null || PreviousMessage == null)
                    return;

                // Skip if the message is the same
                if(_currentMessageText == statusText)
                    return;

                // Move current message to previous
                PreviousMessage.Text = _currentMessageText;
                PreviousMessage.Opacity = CurrentMessage.Opacity;

                // Set new current message
                CurrentMessage.Text = statusText;
                CurrentMessage.Opacity = 0;
                _currentMessageText = statusText;

                System.Diagnostics.Debug.WriteLine($"Status: {statusText}");

                // Animate fade out for previous message (moving up)
                if(!string.IsNullOrEmpty(PreviousMessage.Text))
                {
                    FadeOutStoryboard.Begin();
                }
                NotifyPropertyChanged(nameof(ProgressValue));
                // Small delay before fading in new message
                await Task.Delay(100);

                // Animate fade in for current message
                FadeInStoryboard.Begin();
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Status display error: {ex.Message}");
            }
        }

        private async Task MarkProcessingCompleteAsync()
        {
            if(!_isProcessingComplete)
            {
                _isProcessingComplete = true;
                await CompleteInitializationAsync();
            }
        }

        private async Task CompleteInitializationAsync()
        {
            try
            {
                // ESKÝ: await ExecuteStep("Baþlatýyor", 400, async () =>
                // YENÝ: Doðrudan çaðýr

                // Son progress mesajý
                if(_startupService != null)
                {
                    await _startupService.BeginStepAsync(
                        StartupStep.Complete,
                        "Uygulama baþlatýlýyor...",
                        CancellationToken.None);
                }

                // Ana uygulamayý yükle
                await LoadMainApplicationAsync();

                // Splash'i gizle
                await HideSplash();

                // Step tamamla
                if(_startupService != null)
                {
                    await _startupService.CompleteStepAsync("Uygulama hazýr");
                }
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Complete initialization error: {ex.Message}");

                // Hata durumunda
                if(_startupService != null)
                {
                    await _startupService.FailStepAsync($"Baþlatma hatasý: {ex.Message}", ex);
                }

                await DispatcherQueue.EnqueueAsync(
                    () =>
                    {
                        SplashOverlay.Visibility = Visibility.Collapsed;
                    });
            }
        }

        private async Task LoadMainApplicationAsync()
        {
            try
            {
                var activationInfo = ActivationInfo.CreateDefault();
                var shellArgs = new ShellArgs
                {
                    ViewModel = activationInfo.EntryViewModel,
                    Parameter = activationInfo.EntryArgs,
                    UserInfo = HesapModel.Default
                };
                await DispatcherQueue.EnqueueAsync(
                    () =>
                    {
                        rootFrame.Navigate(typeof(LoginView), shellArgs);
                        WindowHelper.MainWindow.Content = rootFrame;
                        var themeSelectorService = ServiceLocator.Current.GetService<IThemeSelectorService>();
                        if(rootFrame is FrameworkElement element)
                        {
                            element.RequestedTheme = themeSelectorService.Theme;
                        }
                    });
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load main application error: {ex.Message}");
            }
        }

        private async Task HideSplash()
        {
            try
            {
                await DispatcherQueue.EnqueueAsync(
                    () =>
                    {
                        var fadeAnimation = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromMilliseconds(500)
                        };
                        var storyboard = new Storyboard();
                        Storyboard.SetTarget(fadeAnimation, SplashOverlay);
                        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
                        storyboard.Children.Add(fadeAnimation);
                        storyboard.Completed += (s, e) =>
                        {
                            SplashOverlay.Visibility = Visibility.Collapsed;
                        };
                        storyboard.Begin();
                    });
                await Task.Delay(500);
            } catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hide splash error: {ex.Message}");
                await DispatcherQueue.EnqueueAsync(
                    () =>
                    {
                        SplashOverlay.Visibility = Visibility.Collapsed;
                    });
            }
        }

        private async Task ShowNotificationAsync(string message, string title = "Bilgi")
        {
            try
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                DispatcherQueue.TryEnqueue(
                    async () =>
                    {
                        try
                        {
                            if(this?.Content?.XamlRoot != null)
                            {
                                var dialog = new ContentDialog
                                {
                                    Title = title,
                                    Content = message,
                                    PrimaryButtonText = "Tamam",
                                    XamlRoot = this.Content.XamlRoot
                                };
                                await dialog.ShowAsync();
                            }
                            taskCompletionSource.SetResult(true);
                        } catch(Exception ex)
                        {
                            Debug.WriteLine($"Notification failed: {ex.Message}");
                            taskCompletionSource.SetResult(false);
                        }
                    });
                await taskCompletionSource.Task;
            } catch(Exception ex)
            {
                Debug.WriteLine($"Notification failed: {ex.Message}");
            }
        }
    }
}
