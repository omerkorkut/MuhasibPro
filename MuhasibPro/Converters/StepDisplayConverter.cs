using Microsoft.UI.Xaml.Data;
using MuhasibPro.Business.ResultModels.TenantResultModels;

namespace MuhasibPro.Converters
{
    public class StepDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CreationStep step)
            {
                // Status icon'ını oluştur
                string icon = step.Status switch
                {
                    CreationStepStatus.Bekliyor => "⏳",
                    CreationStepStatus.Calisiyor => "🔄",
                    CreationStepStatus.Tamamlandi => "✅",
                    CreationStepStatus.Hata => "❌",
                    CreationStepStatus.Uyari => "⚠️",
                    _ => "●"
                };

                // Adım adını oluştur (modeldeki gibi)
                string stepName = step.Step switch
                {
                    TenantCreationStep.IslemBaslatildi => "İşlem Başlatıldı",
                    TenantCreationStep.MaliYilGecerlilikKontrolu => "Mali Yıl Geçerliliği Kontrol Ediliyor",
                    
                    TenantCreationStep.FirmaBilgileriKontrolu => "Firma Bilgileri Kontrol Ediliyor",
                    TenantCreationStep.MaliDonemZatenVarMiKontrolu => "Mali Dönem Kontrol Ediliyor",
                    TenantCreationStep.MaliDonemZatenVar => "Oluşturmak İstediğiniz Mali Dönem Zaten Var",
                    TenantCreationStep.VeritabaniAdiOlusturuluyor => "Veritabanı Adı Oluşturuluyor",
                    TenantCreationStep.MaliDonemKaydiOlusturuluyor => "Mali Dönem Kaydı Oluşturuluyor",
                    TenantCreationStep.VeritabaniDosyasiOlusturuluyor => "Veritabanı Dosyası Oluşturuluyor",
                    TenantCreationStep.TumIslemlerTamamlandi => "Tüm İşlemler Tamamlandı",
                    TenantCreationStep.BeklenmeyenHata => "Beklenmeyen Hata",
                    TenantCreationStep.TumIslemlerGeriAlindi => "Yapısal Bütünlük Sağlanamadı, Tüm İşlemler Geri Alındı",
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

            if (value is CreationStepStatus status)
            {
                // Sadece status için icon
                return status switch
                {
                    CreationStepStatus.Bekliyor => "⏳",
                    CreationStepStatus.Calisiyor => "🔄",
                    CreationStepStatus.Tamamlandi => "✅",
                    CreationStepStatus.Hata => "❌",
                    CreationStepStatus.Uyari => "⚠️",
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

    public class DurationDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CreationStep step)
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
                return step.Status == CreationStepStatus.Calisiyor ? "..." : "";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}