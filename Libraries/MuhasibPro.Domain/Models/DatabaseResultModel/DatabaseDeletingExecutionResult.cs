using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseDeletingExecutionResult : BaseDatabaseResult
    {
        public bool IsDeletedSuccess { get; set; }
        public override string OperationDisplayName => "Veritabanı Silme";

        public override DatabaseStatusResult GetStatus()
        {
            if (HasError) return DatabaseStatusResult.UnknownError;
            if (IsDeletedSuccess) return DatabaseStatusResult.Healty;

            return DatabaseStatusResult.UnknownError;
        }

        public override string GetStatusMessage()
        {
            if (HasError) return $"Veritabanı silme hatası: {Message}";

            return IsDeletedSuccess
                ? $"{DatabaseName} veritabanı başarıyla silindi."
                : "Veritabanı silme işlemi başarısız";
        }
    }
}
