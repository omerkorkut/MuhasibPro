using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseRestoreExecutionResult : BaseDatabaseResult
    {
        public bool IsRestoreSuccess { get; set; }

        public override string OperationDisplayName => "Onarım";

        public override DatabaseStatusResult GetStatus()
        {
            if (HasError) return DatabaseStatusResult.UnknownError;
            if (IsRestoreSuccess) return DatabaseStatusResult.RestoreCompleted;

            return DatabaseStatusResult.UnknownError;
        }

        public override string GetStatusMessage()
        {
            return IsRestoreSuccess
                ? "Veritabanı yedeği başarıyla geri yüklendi ve doğrulandı."
                : $"Geri yükleme başarısız oldu: {Message}";
        }
    }
}