using MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.SistemDatabaseService
{
    public class SistemDatabaseOperationService : ISistemDatabaseOperationService
    {
        private readonly ISistemBackupManager _backupManager;
        private readonly ISistemLogService _logservice;

        public SistemDatabaseOperationService(ISistemBackupManager backupManager, ISistemLogService logservice)
        {
            _backupManager = backupManager;
            _logservice = logservice;
        }

        public async Task<ApiDataResponse<int>> CleanOldBackupsAsync(int keepLast)
        {
            try
            {
                // BUSINESS VALIDATION 1: keepLast kontrolü
                if (keepLast < 1)
                {
                    return ApiDataExtensions.ErrorResponse<int>(
                        data: -1,
                        message: "En az 1 yedek dosya korunmalıdır!",
                        resultCount: 0);
                }

                // BUSINESS VALIDATION 2: Backup'ları kontrol et
                var backupList = await _backupManager.GetBackupsAsync();

                if (!backupList.Any())
                {
                    await _logservice.SistemLogInformationAsync(
                       "Sistem Veritabanı",
                       "Veritabanı Yedek Listesi",
                       $"Sistem veritabanı için hiç yedek bulunamadı.",
                       "Sistem veritabanı güvenliği için yedek alınması gerekli.");
                    return ApiDataExtensions.ErrorResponse<int>(
                        data: 0,
                        message: "Silinecek yedek bulunamadı.",
                        resultCount: 0);
                }

                var validBackups = backupList.Where(b => b.IsBackupComleted).ToList();
                var invalidBackups = backupList.Where(b => !b.IsBackupComleted).ToList();

                // Geçersiz backup'ları hemen sil
                if (invalidBackups.Any())
                {
                    await _logservice.SistemLogInformationAsync(
                        "Sistem Veritabanı",
                        "Veritabanı Yedek Listesi",
                        $"Sistem veritabanı için {invalidBackups.Count} geçersiz yedek.",
                        "Sistem veritabanı için geçersiz yedekler var. Lütfen sistem yöneticiniz ile iletişime geçiniz!");
                }

                // BUSINESS VALIDATION 3: Geçerli backup sayısı kontrolü
                if (validBackups.Count <= keepLast)
                {
                    var message = validBackups.Count == 0
                        ? "Geçerli backup bulunamadı."
                        : $"Mevcut geçerli backup sayısı ({validBackups.Count}), korunacak sayıdan ({keepLast}) az veya eşit. Silme işlemi yapılmadı.";

                    await _logservice.SistemLogInformationAsync(
                       "Sistem Veritabanı",
                       "Veritabanı Yedek Listesi",
                       $"Yedek detayları işlendi",
                       $"{message}");

                    return ApiDataExtensions.SuccessResponse<int>(
                        data: 0,
                        message: message,
                        resultCount: validBackups.Count);
                }

                // BUSINESS LOGIC: Silme işlemi öncesi log
                var backupsToDelete = validBackups.Count - keepLast;
                var totalSizeMB = validBackups.Skip(keepLast).Sum(b => b.BackupFileSizeBytes) / (1024 * 1024);
                await _logservice.SistemLogInformationAsync("Sistem Veritabanı",
                       "Veritabanı Yedek Listesi",
                       "Yedekler temizleniyor",
                       $"Toplam geçerli yedek {validBackups.Count}\n," +
                       $"Korunacak: {keepLast}\n+" +
                       $"Silinecek: {backupsToDelete}\n" +
                       $"Tahmini boşalan alan: {totalSizeMB}");
               
                // Data katmanı metodunu çağır
                var deletedCount = await _backupManager.CleanOldBackupsAsync(keepLast);

                // BUSINESS LOGIC: Sonuç değerlendirme
                if (deletedCount <= 0)
                {
                    await _logservice.SistemLogErrorAsync("Sistem Veritabanı",
                      "Veritabanı Yedek Listesi",
                      "Hiçbir yedek silinemedi",
                      $"Yedekler silinirken bir hata oluştu");
                    return new ErrorApiDataResponse<int>(
                        data: 0,
                        message: "Yedekler silinirken bir hata oluştu.",
                        resultCount: validBackups.Count);
                }

                // Kalan backup'ları tekrar kontrol et
                var remainingBackups = await _backupManager.GetBackupsAsync();
                var remainingValidBackups = remainingBackups.Where(b => b.IsBackupComleted).ToList();
                await _logservice.SistemLogInformationAsync("Sistem Veritabanı",
                       "Veritabanı Yedek Listesi",
                       "Eski yedekler silindi",
                       $"Silinen yedek sayısı: {deletedCount}\n," +
                       $"Kalan yedek dosya sayısı: {remainingValidBackups.Count}");
              

                // SUCCESS RESPONSE
                return new SuccessApiDataResponse<int>(
                    data: deletedCount,
                    message: $"{deletedCount} adet eski backup başarıyla silindi.\n" +
                            $"Kalan geçerli backup sayısı: {remainingValidBackups.Count}",
                    resultCount: remainingValidBackups.Count);
            }
            catch (OperationCanceledException)
            {
                await _logservice.SistemLogInformationAsync(
                  "Sistem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem durduruldu",
                  "Kullanıcı temizleme işlemini durdurdu");
                return new ErrorApiDataResponse<int>(
                    data: -1,
                    message: "İşlem kullanıcı tarafından iptal edildi.",
                    resultCount: 0);
            }
            catch (Exception ex)
            {
                await _logservice.SistemLogExceptionAsync("Sistem Veritabanı Yedek Listesi", "Veritabanını Yedekleme", ex);
                return new ErrorApiDataResponse<int>(
                    data: -1,
                    message: "Yedek temizleme sırasında teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }


        public async Task<ApiDataResponse<DatabaseBackupResult>> CreateBackupAsync(
            DatabaseBackupType backupType)
        {
            try
            {
                await _logservice.SistemLogInformationAsync(
                   "Sistem Veritabanı Yedek Listesi",
                   "Veritabanını Yedekleme",
                   "İşlem başlatılıyor...",
                   $"Yedekleme Tipi: {backupType}");

                var result = await _backupManager.CreateBackupAsync(backupType);

                if(result.IsBackupComleted)
                {
                    await _logservice.SistemLogInformationAsync(
                  "Sistem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem başarılı",
                  $"Yedeklenen Veritabanı dosyası : {result.BackupFileName} Boyut: {result.BackupFileSizeBytes}");
                    
                    return new SuccessApiDataResponse<DatabaseBackupResult>(
                        data: result,
                        message: result.Message,
                        resultCount: 1);
                } else
                {
                    await _logservice.SistemLogErrorAsync(
                  "Sistem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem başarısız",
                  $"Hata açıklaması : {result.Message}");

                    return new ErrorApiDataResponse<DatabaseBackupResult>(
                        data: result,
                        message: result.Message,
                        resultCount: 0);
                }
            } catch(OperationCanceledException)
            {
                await _logservice.SistemLogInformationAsync(
                  "Sistem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem durduruldu",
                  "Kullanıcı yedekleme işlemini durdurdu");
                return new ErrorApiDataResponse<DatabaseBackupResult>(
                    data: null,
                    message: "Yedek alma işlemi iptal edildi.",
                    resultCount: 0);
            } catch(Exception ex)
            {
                await _logservice.SistemLogExceptionAsync("Sistem Veritabanı Yedek Listesi", "Veritabanını Yedekleme",ex);
                return new ErrorApiDataResponse<DatabaseBackupResult>(
                    data: null,
                    message: "Yedek oluşturulurken teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }

        public async Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync()
        {
            try
            {
                var backupList = new List<DatabaseBackupResult>();

                var backups = await _backupManager.GetBackupsAsync();

                if(!backups.Any())
                {
                    return new SuccessApiDataResponse<List<DatabaseBackupResult>>(
                        data: backupList,
                        message: "Henüz sistem veritabanının yedeği bulunmuyor.",
                        resultCount: 0);
                }

                var validBackups = backups.Where(b => b.IsBackupComleted).ToList();
                var invalidBackups = backups.Where(b => !b.IsBackupComleted).ToList();

                if(invalidBackups.Any())
                {
                    await _logservice.SistemLogInformationAsync(
                        "Sistem Veritabanı Yedek Listesi",
                        "Veritabanı Yedekleme",
                        $"Sistem veritabanı için {invalidBackups.Count} geçersiz yedek.",
                        "Sistem veritabanı için geçersiz yedekler var. Lütfen sistem yöneticiniz ile iletişime geçiniz!");
                }

                await _logservice.SistemLogInformationAsync(
                    "Sistem Veritabanı Yedek Listesi",
                    "Veritabanı Yedekleme",
                    $"{validBackups.Count} yedek bulundu. ({validBackups.Count} geçerli, {invalidBackups.Count} geçersiz)",
                    $"Toplam Yedek : {backups.Count}, Geçerli Yedek : {validBackups.Count}, Geçersiz Yedek : { invalidBackups.Count}");

                return new SuccessApiDataResponse<List<DatabaseBackupResult>>(
                    data: backups,
                    message: $"{validBackups.Count} adet geçerli backup bulundu.",
                    resultCount: backups.Count);
            } catch(Exception ex)
            {
                await _logservice.SistemLogExceptionAsync("Sistem Veritabanı Yedek Listesi", "Veritabanı Yedekleme", ex);
                return new ErrorApiDataResponse<List<DatabaseBackupResult>>(
                    data: null,
                    message: "Veritabanı Yedek geçmişi alınamadı.",
                    resultCount: 0);
            }
        }

        public DateTime? GetLastBackupDate()
        {
            try
            {
                var lastBackupDate = _backupManager.GetLastBackupDate();


                return lastBackupDate;
            } catch(Exception)
            {
                return null;
            }
        }

        public async Task<ApiDataResponse<DatabaseRestoreExecutionResult>> RestoreBackupAsync(
            string backupFileName)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(backupFileName))
                {
                    return new ErrorApiDataResponse<DatabaseRestoreExecutionResult>(
                        data: null,
                        message: "Yedeklenecek dosya adı belirtilmelidir.",
                        resultCount: 0);
                }

                await _logservice.SistemLogInformationAsync(
                    "Sistem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri Yükleme",
                    "İşlem başlatılıyor...",
                    $"Geri yüklenmesi beklenen yedek dosyası: {backupFileName}");


                var result = await _backupManager.RestoreBackupAsync(backupFileName);

                if(result.IsRestoreSuccess)
                {
                    await _logservice.SistemLogInformationAsync(
                        "Sistem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "İşlem başarılı",
                        $"Geri yüklenen yedek dosyası: {backupFileName}");

                    return new SuccessApiDataResponse<DatabaseRestoreExecutionResult>(
                        data: result,
                        message: result.Message,
                        resultCount: 1);
                } else
                {
                    await _logservice.SistemLogErrorAsync(
                        "Sistem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "İşlem başarısız",
                        $"Geri yüklenemeyen yedek dosyası: {backupFileName}");

                    return new ErrorApiDataResponse<DatabaseRestoreExecutionResult>(
                        data: result,
                        message: result.Message ?? "Geri yükleme başarısız oldu.",
                        resultCount: 0);
                }
            } catch(Exception ex)
            {
                await _logservice.SistemLogExceptionAsync(
                    "Sistem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri yükleme",
                    ex);
                return new ErrorApiDataResponse<DatabaseRestoreExecutionResult>(
                    data: null,
                    message: "Geri yükleme sırasında teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }

        public async Task<ApiDataResponse<bool>> RestoreFromLatestBackupAsync()
        {
            try
            {
                await _logservice.SistemLogInformationAsync(
                    "Sistem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri Yükleme",
                    "⏳ Geri yükleme işlemi başlatılıyor.",
                    "Sistem veritabanı son yedeği ile değiştirilecek!");
                var result = await _backupManager.RestoreFromLatestBackupAsync();
                if(result)
                {
                    await _logservice.SistemLogInformationAsync(
                        "Sistem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "✅ Geri yükleme işlemi başarılı.",
                        $"Sistem veritabanı son yedeği ile {DateTime.Now.ToShortDateString} tarihinde değiştirildi");
                    return new SuccessApiDataResponse<bool>(
                        data: result,
                        message: "✅ En son yedekten geri yükleme başarılı");
                } else
                {
                    await _logservice.SistemLogErrorAsync(
                        "Sistem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "❌ Geri yükleme işlemi başarısız.",
                        $"Sistem veritabanı geri yükleme işlemi başarısız oldu!");
                    return new ErrorApiDataResponse<bool>(
                        data: false,
                        message: "❌ Son yedekten geri yükleme işlemi başarısız");
                }
            } catch(Exception ex)
            {
                await _logservice.SistemLogExceptionAsync(
                    nameof(SistemDatabaseOperationService),
                    nameof(RestoreFromLatestBackupAsync),
                    ex);
                return new ErrorApiDataResponse<bool>(data: false, message: $"[HATA]: {ex.Message}");
            }
        }
    }
}
