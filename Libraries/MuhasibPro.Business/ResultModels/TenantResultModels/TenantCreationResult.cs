using System.Text;

namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public enum CreationStepStatus
    {
        Bekliyor,      // ⏳
        Calisiyor,     // 🔄
        Tamamlandi,    // ✅
        Hata,          // ❌
        Uyari          // ⚠️
    }

    public enum TenantCreationStep
    {
        // BAŞLANGIÇ
        IslemBaslatildi,        

        // Ön Kontroller
        MaliDonemZatenVarMiKontrolu,
        FirmaBilgileriKontrolu,
        MaliYilGecerlilikKontrolu,
        MaliDonemZatenVar,

        // İşlemler
        
        VeritabaniAdiOlusturuluyor,
        MaliDonemKaydiOlusturuluyor,
        VeritabaniDosyasiOlusturuluyor,

        // Tamamlananlar     
        TumIslemlerTamamlandi,

        // HATA
        
        BeklenmeyenHata,
        TumIslemlerGeriAlindi,
    }

    public class CreationStep
    {
        public TenantCreationStep Step { get; set; }
        public CreationStepStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;

        public bool IsCompleted => Status == CreationStepStatus.Tamamlandi
                                || Status == CreationStepStatus.Hata
                                || Status == CreationStepStatus.Uyari;
        
    }

    public class TenantCreationResult
    {
        // Ana veriler
        public string DatabaseName { get; set; } = string.Empty;
        public long MaliDonemId { get; set; }
        public long FirmaId { get; set; }        
        public int MaliYil { get; set; }

        // Durumlar
        public bool DatabaseCreated { get; set; }        
        public bool HasError { get; set; }
        public bool CreateCompleted { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        // İşlem akışı
        public List<CreationStep> Steps { get; } = new();
        public CreationStep CurrentStep => Steps.LastOrDefault(s => !s.IsCompleted);
        public bool IsSuccess => CreateCompleted && !HasError  && DatabaseCreated;
        
        // İlerleme
        public int TotalSteps => Steps.Count;
        public int CompletedSteps => Steps.Count(s => s.IsCompleted);
        public int ProgressPercentage => TotalSteps > 0 ? (CompletedSteps * 100) / TotalSteps : 0;

      
     
        public void StartStep(TenantCreationStep step)
        {
            StartStep(step,GetStepDisplayName(step));
        }
        private void StartStep(TenantCreationStep step, string message = "")
        {
            Steps.Add(new CreationStep
            {
                Step = step,
                Status = CreationStepStatus.Calisiyor,
                StartedAt = DateTime.UtcNow,
                Message = message
            });
        }
        public void CompleteStep(CreationStepStatus status, string message = "")
        {
            var current = CurrentStep;
            if (current != null && current.Status == CreationStepStatus.Calisiyor)
            {
                current.Status = status;
                current.CompletedAt = DateTime.UtcNow;
                current.Message = message;
            }
        }

        public void MarkAsError(string error)
        {
            HasError = true;
            ErrorMessage = error;

            // Aktif step'i hata olarak işaretle
            CompleteStep(CreationStepStatus.Hata, error);
        }

        public void MarkAsSuccess(string message = "")
        {
            CreateCompleted = true;
            SuccessMessage = string.IsNullOrEmpty(message)
                ? "✅ Veritabanı başarıyla oluşturuldu"
                : message;
        }
 

        // Kullanıcı dostu görüntüleme
        public string GetProgressDisplay()
        {
            var display = new StringBuilder();

            foreach (var step in Steps)
            {
                var icon = step.Status switch
                {
                    CreationStepStatus.Bekliyor => "⏳",
                    CreationStepStatus.Calisiyor => "🔄",
                    CreationStepStatus.Tamamlandi => "✅",
                    CreationStepStatus.Hata => "❌",
                    CreationStepStatus.Uyari => "⚠️",
                    _ => "❓"
                };

                display.AppendLine($"{icon} {GetStepDisplayName(step.Step)}: {step.Message}");
            }

            if (!string.IsNullOrEmpty(SuccessMessage))
                display.AppendLine($"\n{SuccessMessage}");

            if (!string.IsNullOrEmpty(ErrorMessage))
                display.AppendLine($"\n{ErrorMessage}");

            return display.ToString();
        }

        public List<string> GetSimpleProgressList()
        {
            var list = new List<string>();

            foreach (var step in Steps.Where(s => s.IsCompleted))
            {
                var icon = step.Status switch
                {
                    CreationStepStatus.Tamamlandi => "✅",
                    CreationStepStatus.Hata => "❌",
                    CreationStepStatus.Uyari => "⚠️",
                    _ => ""
                };

                list.Add($"{icon} {GetStepDisplayName(step.Step)}");
            }

            return list;
        }

        private static string GetStepDisplayName(TenantCreationStep step)
        {
            return step switch
            {
                // BAŞLANGIÇ
                TenantCreationStep.IslemBaslatildi => "⏳ İşlem Başlatıldı",
                

                // VALİDASYON
                TenantCreationStep.FirmaBilgileriKontrolu => " 🗃️ Firma Bilgileri Kontrol Ediliyor",
                TenantCreationStep.MaliYilGecerlilikKontrolu => "🧮 Mali Yıl Geçerliliği Kontrol Ediliyor",
                TenantCreationStep.MaliDonemZatenVarMiKontrolu => $"🟢 Mali Dönem Kontrol Ediliyor",
                TenantCreationStep.MaliDonemZatenVar => $"🟢 Oluşturmak İstediğiniz Mali Dönem Zaten Var.",

                // İşlemler
                
                TenantCreationStep.VeritabaniAdiOlusturuluyor => "📝 Veritabanı Adı Oluşturuluyor",
                TenantCreationStep.MaliDonemKaydiOlusturuluyor => "➕ Mali Dönem Kaydı Oluşturuluyor",
                TenantCreationStep.VeritabaniDosyasiOlusturuluyor => "🔗 Veritabanı Dosyası Oluşturuluyor",


                // İşlem Sonucları
             
                TenantCreationStep.TumIslemlerTamamlandi => "✅ Tüm İşlemler Tamamlandı",

        
                TenantCreationStep.TumIslemlerGeriAlindi => "❌ Yapısal bütünlük sağlanamadı, Tüm işlemler geri alındı",
                
                TenantCreationStep.BeklenmeyenHata => "❗ Beklenmeyen Hata",

                _ => step.ToString()
            };
        }

        // Quick status check (eski GetStatus yerine)
        public string GetOverallStatus()
        {
            if (HasError) return "❌ İşlem Başarısız";
            if (CreateCompleted) return "✅ İşlem Tamamlandı";
            if (CurrentStep != null) return $"🔄 {GetStepDisplayName(CurrentStep.Step)}";
            return "⏳ Bekleniyor";
        }

   
    }
}