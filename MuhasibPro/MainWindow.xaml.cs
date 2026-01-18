using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Windows.UI;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MuhasibPro
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {

        private readonly DispatcherQueue _dispatcherQueue;
        private bool _hasLogs = false;
        private bool _isAnalyzing = false;
        private ObservableCollection<LogMessageViewModel> _logMessages;
        private bool _showDetails = false;
        private bool _showResults = false;

        public MainWindow()
        {
            InitializeComponent();
            MigrationManager = Ioc.Default.GetService<ISistemMigrationManager>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            LogMessages = new ObservableCollection<LogMessageViewModel>();

            // ItemsSource'u manuel baðla
            LogMessagesControl.ItemsSource = LogMessages;


            // Data binding
            LogMessagesControl.ItemsSource = _logMessages;
             
            
            analizLog.Text = "Analiz Loglarý";
            // Initial state
            UpdateLogStatus();
        }

        public event PropertyChangedEventHandler? PropertyChanged;



        private void AddLogMessage(string message, Color color)
        {
            var logMessage = new LogMessageViewModel
            {
                Message = message,
                Timestamp = DateTime.Now,
                BackgroundColor = Colors.Transparent,
                ForegroundColor = color
            };

            _logMessages.Add(logMessage);
            UpdateLogStatus();

            // Otomatik scroll
            ScrollToBottom();
        }

        private void ClearLogs()
        {
            _logMessages.Clear();
            UpdateLogStatus();
        }

        private void HandleProgressUpdate(AnalysisProgress progress)
        {
            // Progress bar güncelle
            OverallProgressBar.Value = progress.Percentage;

            // Log mesajý ekle
            var color = progress.Type switch
            {
                ProgressType.Success => Colors.Green,
                ProgressType.Warning => Colors.Orange,
                ProgressType.Error => Colors.Red,
                _ => Colors.Black
            };

            // Mesaj formatý
            var formattedMessage = $"[{progress.Percentage}%] {progress.Message}";
            AddLogMessage(formattedMessage, color);


        }
        private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
        {
            if (IsAnalyzing) return;

            await StartAnalysisAsync();
        }

        private void OnClearLogClick(object sender, RoutedEventArgs e)
        {
            ClearLogs();
        }

        private async Task ProcessAnalysisResultsAsync(DatabaseHealtyDiagReport analysis)
        {
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                ShowResults = true;

                // Baðlantý durumu
                ConnectionStatusText.Text = analysis.CanConnect ? "Baðlantý Var" : "Baðlantý Yok";
                ConnectionStatusText.Foreground = analysis.CanConnect ?
                    new SolidColorBrush(Colors.Green) :
                    new SolidColorBrush(Colors.Red);

                // Tablo sayýsý
                TableCountText.Text = analysis.TableCount.ToString();

                // Veritabaný durumu
                if (analysis.HasError)
                {
                    DatabaseStatusText.Text = "Hata";
                    DatabaseStatusText.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (analysis.IsEmptyDatabase)
                {
                    DatabaseStatusText.Text = "Boþ DB";
                    DatabaseStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    DatabaseStatusText.Text = "Saðlýklý";
                    DatabaseStatusText.Foreground = new SolidColorBrush(Colors.Green);
                }

                // Migration durumu
                MigrationStatusText.Text = $"{analysis.AppliedMigrationsCount}/{(analysis.PendingMigrations?.Count ?? 0) + analysis.AppliedMigrationsCount}";

                // Detaylý bilgiler
                ShowDetails = true;
                DatabaseNameText.Text = analysis.DatabaseName;
                VersionText.Text = analysis.CurrentVersion ?? "Bilinmiyor";
                IsEmptyText.Text = analysis.IsEmptyDatabase ? "Evet" : "Hayýr";
                HasErrorText.Text = analysis.HasError ? "Evet" : "Hayýr";
                AnalysisTimeText.Text = analysis.OperationTime.ToString("yyyy-MM-dd HH:mm:ss");

                // Sonuç özeti
                string summary = analysis.HasError
                    ? $"Analiz hata ile tamamlandý: {analysis.Message}"
                    : $"Analiz baþarýyla tamamlandý: {analysis.TableCount} tablo, {analysis.AppliedMigrationsCount} migration";

                AddLogMessage($"=== SONUÇ: {summary} ===", Colors.DarkBlue);
            });
        }

        private void ResetResultsUI()
        {
            ConnectionStatusText.Text = "Bilinmiyor";
            TableCountText.Text = "0";
            DatabaseStatusText.Text = "Bilinmiyor";
            MigrationStatusText.Text = "0/0";

            DatabaseNameText.Text = "";
            VersionText.Text = "";
            IsEmptyText.Text = "";
            HasErrorText.Text = "";
            AnalysisTimeText.Text = "";
        }

        private void ScrollToBottom()
        {
            if (LogMessagesControl.Items.Count > 0)
            {
                var lastItem = LogMessagesControl.Items[LogMessagesControl.Items.Count - 1];
                LogMessagesControl.ScrollIntoView(lastItem);
            }
        }

        private async Task StartAnalysisAsync()
        {
            IsAnalyzing = true;
            ClearLogs();
            AddLogMessage("Analiz baþlatýlýyor...", Colors.Blue);

            // Show progress
            OverallProgressBar.Visibility = Visibility.Visible;
            OverallProgressBar.Value = 0;

            try
            {
                var progress = new Progress<AnalysisProgress>();
                progress.ProgressChanged += (sender, p) =>
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        // Daha görünür olmasý için delay ekle
                        Task.Delay(50).ContinueWith(_ =>
                        {
                            _dispatcherQueue.TryEnqueue(() =>
                            {
                                HandleProgressUpdate(p);
                            });
                        });
                    });
                };

                // Daha yavaþ analiz için options
                var options = new AnalysisOptions
                {
                    CheckIntegrity = true,
                    CheckMigrations = true,
                    BatchSize = 1, // Her seferinde 1 tablo (daha fazla progress)
                    DelayBetweenBatches = TimeSpan.FromMilliseconds(300), // Daha uzun delay
                };

                AddLogMessage("Veritabaný analizi baþlýyor...", Colors.DarkBlue);

                // Yavaþ baþlangýç için manuel progress
                for (int i = 0; i <= 10; i++)
                {
                    ((IProgress<AnalysisProgress>)progress).Report(new AnalysisProgress
                    {
                        Message = $"Baþlatýlýyor... %{i * 10}",
                        Percentage = i * 10,
                        Type = ProgressType.Info,
                        Timestamp = DateTime.UtcNow
                    });
                    await Task.Delay(150);
                }

                // Gerçek analiz
                var analysis = await MigrationManager.GetSistemDatabaseFullDiagStateAsync(
                    progressReporter: progress,
                    options: options);

                // Yavaþ bitiþ için
                for (int i = 90; i <= 100; i++)
                {
                    ((IProgress<AnalysisProgress>)progress).Report(new AnalysisProgress
                    {
                        Message = $"Sonuçlar iþleniyor... %{i}",
                        Percentage = i,
                        Type = ProgressType.Success,
                        Timestamp = DateTime.UtcNow
                    });
                    await Task.Delay(100);
                }

                await ProcessAnalysisResultsAsync(analysis);
            }
            catch (Exception ex)
            {
                AddLogMessage($"Hata: {ex.Message}", Colors.Red);
            }
            finally
            {
                IsAnalyzing = false;
                OverallProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateLogStatus()
        {
            HasLogs = _logMessages.Count > 0;
        }

        public bool HasLogs
        {
            get => _hasLogs;
            set
            {
                if (_hasLogs != value)
                {
                    _hasLogs = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasLogs)));
                }
            }
        }
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                if (_isAnalyzing != value)
                {
                    _isAnalyzing = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAnalyzing)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotAnalyzing)));
                }
            }
        }

        public bool IsNotAnalyzing => !_isAnalyzing;
        public ObservableCollection<LogMessageViewModel> LogMessages
        {
            get => _logMessages;
            set
            {
                _logMessages = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogMessages)));
            }
        }
        public ISistemMigrationManager MigrationManager { get; private set; }

        public bool ShowDetails
        {
            get => _showDetails;
            set
            {
                if (_showDetails != value)
                {
                    _showDetails = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDetails)));
                }
            }
        }

        public bool ShowResults
        {
            get => _showResults;
            set
            {
                if (_showResults != value)
                {
                    _showResults = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowResults)));
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MigrationManager.InitializeSistemDatabaseAsync(cancellationToken: CancellationToken.None);

        }
    }

    // ViewModel
    public class LogMessageViewModel
    {
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

