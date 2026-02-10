// Services/UIService/StartupApplicationService.cs
using MuhasibPro.Contracts.UIService;
using MuhasibPro.Services.ServiceExtensions.StartupApplication;

namespace MuhasibPro.Services.UIService
{
    public class StartupApplicationService : IStartupApplicationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly object _syncLock = new();

        private StartupState _currentState = StartupState.NotStarted;
        private double _currentProgress = 0;
        private string _currentMessage = string.Empty;
        private StartupStep? _currentStep = null;
        private bool _isStepActive = false;
        private Dictionary<StartupStep, double> _stepProgressMap;

        public event EventHandler<StartupProgressEventArgs> ProgressChanged;

        public StartupState CurrentState
        {
            get
            {
                lock(_syncLock)
                    return _currentState;
            }
            private set
            {
                lock(_syncLock)
                    _currentState = value;
            }
        }

        public double CurrentProgress
        {
            get
            {
                lock(_syncLock)
                    return _currentProgress;
            }
            private set
            {
                lock(_syncLock)
                    _currentProgress = value;
            }
        }

        public string CurrentMessage
        {
            get
            {
                lock(_syncLock)
                    return _currentMessage;
            }
            private set
            {
                lock(_syncLock)
                    _currentMessage = value;
            }
        }

        public StartupApplicationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            InitializeStepProgressMap();
        }

        private void InitializeStepProgressMap()
        {
            _stepProgressMap = new Dictionary<StartupStep, double>
            {
                
                [StartupStep.NavigationConfiguration] = 10,
                [StartupStep.DatabaseValidation] = 40,
                [StartupStep.DatabaseInitialization] = 50,
                [StartupStep.DatabaseUpdating] = 70,
                [StartupStep.ServiceInitialization] = 90,
                [StartupStep.ApplicationUpdateCheck] = 99,
                [StartupStep.Complete] = 100
            };
        }

        // StartupApplicationService.cs'deki InitializeAsync()'ı geri değiştirin:
        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            return await this.ExecuteStartupSequenceAsync(_serviceProvider, cancellationToken: cancellationToken);
        }

        public async Task BeginStepAsync(
            StartupStep step,
            string startMessage,
            CancellationToken cancellationToken = default)
        {
            if(_isStepActive)
                throw new InvalidOperationException($"Zaten aktif bir step var: {_currentStep}");

            _currentStep = step;
            _isStepActive = true;

            CurrentState = GetStateForStep(step);
            CurrentMessage = startMessage;
            CurrentProgress = GetStepStartProgress(step);

            ReportProgress();
            await Task.Delay(100, cancellationToken); // UI görmesi için
        }

        public async Task ReportSubProgressAsync(
            string message,
            double subProgress,
            CancellationToken cancellationToken = default)
        {
            if(!_isStepActive || !_currentStep.HasValue)
                throw new InvalidOperationException("Aktif bir step yok");

            // Sub-progress (0-100) → Global progress'e çevir
            double stepStart = GetStepStartProgress(_currentStep.Value);
            double stepEnd = GetStepEndProgress(_currentStep.Value);
            double globalProgress = stepStart + ((stepEnd - stepStart) * (subProgress / 100.0));

            CurrentMessage = message;
            CurrentProgress = globalProgress;

            ReportProgress();
            await Task.Delay(50, cancellationToken); // Smooth görüntü
        }

        public async Task CompleteStepAsync(string completionMessage, CancellationToken cancellationToken = default)
        {
            if(!_isStepActive || !_currentStep.HasValue)
                throw new InvalidOperationException("Aktif bir step yok");

            CurrentMessage = completionMessage;
            CurrentProgress = GetStepEndProgress(_currentStep.Value);

            ReportProgress();

            _isStepActive = false;
            await Task.Delay(300, cancellationToken); // Kullanıcının görmesi için
        }

        public async Task FailStepAsync(
            string errorMessage,
            Exception error = null,
            CancellationToken cancellationToken = default)
        {
            CurrentState = StartupState.Failed;
            CurrentMessage = errorMessage;

            ReportProgress(error: error);

            _isStepActive = false;
            await Task.Delay(500, cancellationToken);
        }

        private StartupState GetStateForStep(StartupStep step)
        {
            return step switch
            {
                StartupStep.DatabaseValidation or
                StartupStep.DatabaseInitialization => StartupState.DatabaseConnecting,

                StartupStep.DatabaseUpdating => StartupState.DatabaseUpdating,

                StartupStep.ServiceInitialization => StartupState.ServicesStarting,

                StartupStep.ApplicationUpdateCheck => StartupState.UpdateChecking,

                _ => StartupState.NotStarted
            };
        }

        private double GetStepStartProgress(StartupStep step)
        {
            // Önceki step'lerin toplam progress'i
            double total = 0;

            foreach(var kvp in _stepProgressMap)
            {
                if(kvp.Key == step)
                    break;

                total = kvp.Value;
            }

            return total;
        }

        private double GetStepEndProgress(StartupStep step)
        { return _stepProgressMap.TryGetValue(step, out var progress) ? progress : 0; }

        private void ReportProgress(Exception error = null)
        {
            var args = new StartupProgressEventArgs(CurrentState, CurrentMessage, CurrentProgress, _currentStep);

            if(error != null)
            {
                args.Error = error;
            }

            // Thread-safe event invocation
            EventHandler<StartupProgressEventArgs> handler;
            lock(_syncLock)
            {
                handler = ProgressChanged;
            }

            handler?.Invoke(this, args);
        }
    }
}