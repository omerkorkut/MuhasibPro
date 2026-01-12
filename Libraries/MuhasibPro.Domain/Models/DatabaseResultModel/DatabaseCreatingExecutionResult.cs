using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public class DatabaseCreatingExecutionResult : BaseDatabaseResult
    {
        public bool IsCreatedSuccess { get; set; }
        public override string OperationDisplayName => "Veritabanı Oluşturma";

        public override DatabaseStatusResult GetStatus()
        {
            if (HasError) return DatabaseStatusResult.UnknownError;
            if (IsCreatedSuccess && CanConnect) return DatabaseStatusResult.Healty;
            if (!CanConnect) return DatabaseStatusResult.ConnectionFailed;

            return DatabaseStatusResult.UnknownError;
        }

        public override string GetStatusMessage()
        {
            if (HasError) return $"Veritabanı oluşturulurken hata: {Message}";

            return IsCreatedSuccess
                ? $"{DatabaseName} veritabanı başarıyla oluşturuldu ve yapılandırıldı."
                : "Veritabanı oluşturma işlemi tamamlanamadı.";
        }
    }
}