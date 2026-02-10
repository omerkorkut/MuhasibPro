// Contracts/UIService/IStartupApplicationService.cs
using System;

namespace MuhasibPro.Contracts.UIService
{
    /// <summary>
    /// Uygulama başlangıç sürecini yöneten servis
    /// </summary>
    public interface IStartupApplicationService
    {
        /// <summary>
        /// Başlangıç ilerlemesi değiştiğinde tetiklenen event
        /// </summary>
        event EventHandler<StartupProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Uygulama başlangıç durumunu getirir
        /// </summary>
        StartupState CurrentState { get; }

        /// <summary>
        /// Mevcut ilerleme yüzdesi (0-100)
        /// </summary>
        double CurrentProgress { get; }

        /// <summary>
        /// Mevcut durum mesajı
        /// </summary>
        string CurrentMessage { get; }

        /// <summary>
        /// Uygulama başlangıç sürecini başlatır (ana method)
        /// </summary>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Bir step'i manuel olarak başlatır
        /// </summary>
        Task BeginStepAsync(StartupStep step, string startMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktif step'in alt işleminin progress'ini bildirir
        /// </summary>
        Task ReportSubProgressAsync(string message, double subProgress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktif step'i başarıyla tamamlandı olarak işaretler
        /// </summary>
        Task CompleteStepAsync(string completionMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktif step'i başarısız olarak işaretler
        /// </summary>
        Task FailStepAsync(string errorMessage, Exception error = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Başlangıç süreci adımları
    /// </summary>
    public enum StartupStep
    {
        ThemeInitialization,
        NavigationConfiguration,
        DatabaseValidation,
        DatabaseInitialization,
        DatabaseUpdating,
        ServiceInitialization,
        ApplicationUpdateCheck,
        Complete
    }

    /// <summary>
    /// Başlangıç durumu
    /// </summary>
    public enum StartupState
    {
        NotStarted,
        DatabaseConnecting,
        DatabaseUpdating,
        ServicesStarting,
        UpdateChecking,
        Completed,
        Failed
    }

    /// <summary>
    /// Başlangıç ilerlemesi event argument'leri
    /// </summary>
    public class StartupProgressEventArgs : EventArgs
    {
        public StartupProgressEventArgs(
            StartupState state,
            string message,
            double progress,
            StartupStep? currentStep = null)
        {
            State = state;
            Message = message;
            Progress = progress;
            CurrentStep = currentStep;
            Timestamp = DateTimeOffset.Now;
        }

        public StartupState State { get; }
        public string Message { get; }
        public double Progress { get; }
        public StartupStep? CurrentStep { get; }
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// İsteğe bağlı: Hata durumunda exception
        /// </summary>
        public Exception Error { get; set; }
    }
}