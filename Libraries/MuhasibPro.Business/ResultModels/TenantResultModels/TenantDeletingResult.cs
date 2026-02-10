using System.Text;

namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public enum DeletionStepStatus
    {
        Bekliyor,      // ⏳
        Calisiyor,     // 🔄
        Tamamlandi,    // ✅
        Hata,          // ❌
        Uyari          // ⚠️
    }
    public enum TenantDeletionStep
    {
        // BAŞLANGIÇ
        IslemBaslatildi,

        // Kontroller
        
        MaliDonemVarmiKontrolu,
        

        // İşlemler
        
        VeritabaniVarmiKontrolEdiliyor,
        MaliDonemKaydiSiliniyor,
        VeritabaniDosyasiSiliniyor,
        VeritabaniYedekleriSiliniyor,
        VeritabaniSilmedenOnceYedekAliniyor,

        // Tamamlananlar
        VeritabaniSilmeIslemiTamamlandi,
        MaliDonemKaydiSilmeIslemiTamamlandi,
        TumIslemlerTamamlandi,

        // HATA
        
        YedekleriSilmeHatasi,
        VeritabaniSilmeIslemiGeriAl,
        MaliDonemKaydiSilmeIslemiGeriAl,
        TumIslemlerGeriAlindi,
        BeklenmeyenHata
    }
    public class DeletionStep
    {
        public TenantDeletionStep Step { get; set; }
        public DeletionStepStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
        public bool IsCompleted => Status == DeletionStepStatus.Tamamlandi
                             || Status == DeletionStepStatus.Hata
                             || Status == DeletionStepStatus.Uyari;
    }
    public class TenantDeletingResult
    {
        public string DatabaseName { get; set; } = string.Empty;
        public long MaliDonemId { get; set; }
        public string DatabaseFilePath { get; set; }
        public List<string> DeletedBackupFiles { get; set; } = new List<string>();
        public string BackupFilePath { get; set; }
        // Durumlar        
        public int DeletedBackupCount { get; set; }
        public bool DatabaseDeleted { get; set; }
        public bool MaliDonemDeleted { get; set; }
        public bool IsCurrentTenantDeletingBeforeBackup { get; set; }     
        public bool DeleteCompleted { get; set; }
        public bool BackupCreateCompleted { get; set; }
        public bool BackupDeleteCompleted { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
     

        // İşlem akışı
        public List<DeletionStep> Steps { get; } = new();
        public DeletionStep CurrentStep => Steps.LastOrDefault(s => !s.IsCompleted);
        public bool HasError { get; set; }
        public bool IsSuccess => DeleteCompleted && !HasError  && MaliDonemDeleted && DatabaseDeleted;

        // İlerleme
        public int TotalSteps => Steps.Count;
        public int CompletedSteps => Steps.Count(s => s.IsCompleted);
        public int ProgressPercentage => TotalSteps > 0 ? (CompletedSteps * 100) / TotalSteps : 0;

        // Yardımcı metodlar
        public void StartStep(TenantDeletionStep step)
        {
            StartStep(step, GetStepDisplayName(step));
        }
        private void StartStep(TenantDeletionStep step, string message = "")
        {
            Steps.Add(new DeletionStep
            {
                Step = step,
                Status = DeletionStepStatus.Calisiyor,
                StartedAt = DateTime.UtcNow,
                Message = message
            });
        }

        public void CompleteStep(DeletionStepStatus status, string message = "")
        {
            var current = CurrentStep;
            if (current != null && current.Status == DeletionStepStatus.Calisiyor)
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
            CompleteStep(DeletionStepStatus.Hata, error);
        }

        public void MarkAsSuccess(string message = "")
        {
            DeleteCompleted = true;
            SuccessMessage = string.IsNullOrEmpty(message)
                ? "✅ Veritabanı başarıyla silindi"
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
                    DeletionStepStatus.Bekliyor => "⏳",
                    DeletionStepStatus.Calisiyor => "🔄",
                    DeletionStepStatus.Tamamlandi => "✅",
                    DeletionStepStatus.Hata => "❌",
                    DeletionStepStatus.Uyari => "⚠️",
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
        private static string GetStepDisplayName(TenantDeletionStep step)
        {
            return step switch
            {
                // BAŞLANGIÇ
                TenantDeletionStep.IslemBaslatildi => "İşlem Başlatıldı",                // VALİDASYON




                // Silme

                TenantDeletionStep.MaliDonemKaydiSiliniyor => "Mali Dönem Kaydı Siliniyor",
                TenantDeletionStep.VeritabaniVarmiKontrolEdiliyor => "Veritabanı Varliğı Kontrol Ediliyor",
                TenantDeletionStep.VeritabaniDosyasiSiliniyor => "Veritabanı Dosyası Siliniyor",
                TenantDeletionStep.VeritabaniSilmedenOnceYedekAliniyor => "Veritabanı Yedek Alma İşlemi Çalıştırılıyor",
                TenantDeletionStep.VeritabaniYedekleriSiliniyor => "Veritabanına Ait Tüm Yedek Veritabanları Siliniyor",


                //Gerial
                TenantDeletionStep.VeritabaniSilmeIslemiGeriAl => "Veritabanı silme işlemi tamamlanamadı, Geri alma işlemi başlatıldı",
                TenantDeletionStep.MaliDonemKaydiSilmeIslemiGeriAl => "Mali Dönem silme işlemi tamamlanamadı, Geri alma işlemi başlatıldı",

                // TAMAMLAMA
                TenantDeletionStep.VeritabaniSilmeIslemiTamamlandi => "Veritabanı başarıyla silindi",
                TenantDeletionStep.MaliDonemKaydiSilmeIslemiTamamlandi => "Mali Dönem kaydı başarıyla silindi",
                TenantDeletionStep.TumIslemlerTamamlandi => "Tüm İşlemler Tamamlandı",

                // HATA
                TenantDeletionStep.YedekleriSilmeHatasi => "Veritabanına Ait Yedekleri Silme Hatası",
                TenantDeletionStep.BeklenmeyenHata => "Beklenmeyen Hata",

                _ => step.ToString()
            };
        }

        // Quick status check (eski GetStatus yerine)
        public string GetOverallStatus()
        {
            if (HasError) return "❌ İşlem Başarısız";
            if (DeleteCompleted) return "✅ İşlem Tamamlandı";
            if (CurrentStep != null) return $"🔄 {GetStepDisplayName(CurrentStep.Step)}";
            return "⏳ Bekleniyor";
        }

    }
}
