using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.Extensions
{
    /// <summary>
    /// BaseDatabaseResult result = await _manager.SomeOperationAsync();

    // Tek bir satırla her şeyi hallet!
    ///this.StatusText = result.ToUIFullMessage();
    ///this.StatusColor = result.GetStatus().GetColor()
    /// </summary>
    public static class DatabaseResultExtensions
    {
        // 1. İkon Yönetimi
        public static string GetIcon(this DatabaseStatusResult status)
        {
            return status switch
            {
                DatabaseStatusResult.Success => "✅",
                DatabaseStatusResult.RestoreCompleted => "📁✅",
                DatabaseStatusResult.RequiredUpdating => "⚠️",
                DatabaseStatusResult.DatabaseNotFound => "📁❌",
                DatabaseStatusResult.ConnectionFailed => "🔴",
                DatabaseStatusResult.InvalidSchema => "🧩",
                DatabaseStatusResult.UnknownError => "❌",
                _ => "❓"
            };
        }

        // 2. Renk Yönetimi (Hex Kodları)
        public static string GetColor(this DatabaseStatusResult status)
        {
            return status switch
            {
                DatabaseStatusResult.Success => "#28a745",          // Yeşil
                DatabaseStatusResult.RequiredUpdating => "#ffc107",   // Sarı
                DatabaseStatusResult.DatabaseNotFound => "#fd7e14", // Turuncu
                _ => "#dc3545"                                      // Kırmızı
            };
        }

        // 3. UI İçin Full Formatlanmış Mesaj (Örn: [Kurulum] ✅ Mesaj...)
        public static string ToUIFullMessage(this BaseDatabaseResult result)
        {
            var status = result.GetStatus();
            return $"{result.GetTaggedMessage().Replace("]", $"] {status.GetIcon()}")}";
        }
        public static (bool IsValid, string Message) ToLegacyResult(this BaseDatabaseResult result)
        {
            // Senin istediğin eşleşmeyi burada tek bir merkezde yapıyoruz
            return (result.IsHealthy, result.ToUIFullMessage());
        }
    }
}
