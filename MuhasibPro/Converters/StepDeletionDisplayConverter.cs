using Microsoft.UI.Xaml.Data;
using MuhasibPro.Business.ResultModels.TenantResultModels;

namespace MuhasibPro.Converters
{
    public class StepDeletionDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DeletionStep step)
            {
                // Status icon'ını oluştur
                string icon = step.Status switch
                {
                    DeletionStepStatus.Bekliyor => "⏳",
                    DeletionStepStatus.Calisiyor => "🔄",
                    DeletionStepStatus.Tamamlandi => "✅",
                    DeletionStepStatus.Hata => "❌",
                    DeletionStepStatus.Uyari => "⚠️",
                    _ => "●"
                };

                // Adım adını oluştur (modeldeki gibi)
                string stepName = step.Step switch
                {
                    TenantDeletionStep.IslemBaslatildi => "İşlem Başlatıldı",
                    TenantDeletionStep.VeritabaniVarmiKontrolEdiliyor => "Veritabanı Kontrol Ediliyor",
                    TenantDeletionStep.MaliDonemVarmiKontrolu => "Mali Dönem Bilgileri Kontrol Ediliyor",
                    TenantDeletionStep.VeritabaniDosyasiSiliniyor => "Veritabanı Dosyası Siliniyor",
                    TenantDeletionStep.VeritabaniYedekleriSiliniyor => "Veritabanı Yedekleri Siliniyor",
                    TenantDeletionStep.MaliDonemKaydiSiliniyor => "Mali Dönem Kaydı Siliniyor",
                    TenantDeletionStep.VeritabaniSilmedenOnceYedekAliniyor => "Veritabanı Silmeden Önce Yedek Alınıyor",
                    TenantDeletionStep.VeritabaniSilmeIslemiTamamlandi => "Veritabanı Silme İşlemi Tamamlandı",
                    TenantDeletionStep.MaliDonemKaydiSilmeIslemiTamamlandi => "Mali Dönem Kaydı Silme İşlemi Tamamlandı",
                    TenantDeletionStep.MaliDonemKaydiSilmeIslemiGeriAl => "Mali Dönem Kaydı Silme İşlemi Geri Alındı",
                    TenantDeletionStep.VeritabaniSilmeIslemiGeriAl => "Veritabanı Silme İşlemi Geri Alındı",
                    TenantDeletionStep.YedekleriSilmeHatasi => "Veritabanı yedeklerini silme hatası",
                    
                    TenantDeletionStep.TumIslemlerTamamlandi => "Tüm İşlemler Tamamlandı",
                    TenantDeletionStep.BeklenmeyenHata => "Beklenmeyen Hata",
                    TenantDeletionStep.TumIslemlerGeriAlindi => "Yapısal Bütünlük Sağlanamadı, Tüm İşlemler Geri Alındı",
                    _ => step.Step.ToString()
                };

                // Parameter'a göre farklı değer döndür
                return parameter switch
                {
                    "IconOnly" => icon,
                    "NameOnly" => stepName,
                    "IconAndName" => $"{icon} {stepName}",
                    _ => $"{icon} {stepName}"
                };
            }

            if (value is DeletionStepStatus status)
            {
                // Sadece status için icon
                return status switch
                {
                    DeletionStepStatus.Bekliyor => "⏳",
                    DeletionStepStatus.Calisiyor => "🔄",
                    DeletionStepStatus.Tamamlandi => "✅",
                    DeletionStepStatus.Hata => "❌",
                    DeletionStepStatus.Uyari => "⚠️",
                    _ => "●"
                };
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class DurationDeletionDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DeletionStep step)
            {
                if (step.Duration.HasValue)
                {
                    var duration = step.Duration.Value;
                    if (duration.TotalSeconds < 1)
                        return $"{(int)(duration.TotalMilliseconds)}ms";
                    else if (duration.TotalSeconds < 60)
                        return $"{duration.TotalSeconds:F1}s";
                    else
                        return $"{duration.TotalMinutes:F1}d";
                }
                return step.Status == DeletionStepStatus.Calisiyor ? "..." : "";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
