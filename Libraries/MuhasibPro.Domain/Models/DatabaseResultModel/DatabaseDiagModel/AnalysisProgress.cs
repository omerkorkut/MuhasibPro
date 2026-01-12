namespace MuhasibPro.Domain.Models.DatabaseResultModel.DatabaseDiagModel
{
    public class AnalysisProgress
    {
        public string Message { get; set; }
        public ProgressType Type { get; set; }
        public double Percentage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum ProgressType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class AnalysisOptions
    {
        public static AnalysisOptions Default => new()
        {
            DatabaseSize = 0,
            CheckIntegrity = true,
            CheckMigrations = true,
            BatchSize = 5,
            DelayBetweenBatches = TimeSpan.FromMilliseconds(50),
            ThrottleUIUpdates = true
            
        };

        public long DatabaseSize { get; set; }
        public bool CheckIntegrity { get; set; }
        public bool CheckMigrations { get; set; }
        public int BatchSize { get; set; }
        public TimeSpan DelayBetweenBatches { get; set; }
        public bool ThrottleUIUpdates { get; set; }
    }
}
