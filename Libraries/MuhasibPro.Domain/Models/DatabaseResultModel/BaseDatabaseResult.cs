using MuhasibPro.Domain.Enum.DatabaseEnum;

namespace MuhasibPro.Domain.Models.DatabaseResultModel
{
    public abstract class BaseDatabaseResult
    {
        // Temel Veriler
        public string DatabaseName { get; set; }
        public bool HasError { get; set; }
        public string Message { get; set; } // Hata detayları için
        public bool CanConnect { get; set; }
        public virtual bool IsUpdateRequired { get; set; } = false;
        public DateTime OperationTime { get; set; } = DateTime.UtcNow;

        // Zorunlu Tanımlamalar (Derived sınıflar dolduracak)
        public abstract string OperationDisplayName { get; }
        public abstract DatabaseStatusResult GetStatus();
        public abstract string GetStatusMessage();

        // Akıllı Yardımıcılar
        public virtual bool IsHealthy => !HasError && CanConnect;

        // Senin istediğin etiketli merkezi mesaj
        public string GetTaggedMessage() => $"[{OperationDisplayName}] {GetStatusMessage()}";

    }
}
