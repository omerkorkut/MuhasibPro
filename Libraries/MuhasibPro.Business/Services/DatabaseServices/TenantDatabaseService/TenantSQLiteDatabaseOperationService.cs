using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Domain.Enum.DatabaseEnum;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    public class TenantSQLiteDatabaseOperationService : ITenantSQLiteDatabaseOperationService
    {
        private readonly ITenantSQLieBackupManager _backupManager;
        private readonly ISistemLogService _logService;

        public TenantSQLiteDatabaseOperationService(ITenantSQLieBackupManager backupManager, ISistemLogService logService)
        {
            _backupManager = backupManager;
            _logService = logService;
        }
        public async Task<ApiDataResponse<int>> CleanOldBackupsAsync(string databaseName, int keepLast, CancellationToken cancellationToken = default)
        {
            try
            {
                if (keepLast < 1)
                {
                    return new ErrorApiDataResponse<int>(
                        data: -1,
                        message: "En az 1 yedek dosya korunmalıdır!",
                        resultCount: 0);
                }
                var backupList = await _backupManager.GetBackupsAsync(databaseName);
                if (!backupList.Any())
                {
                    await _logService.SistemLogInformationAsync(
                           "Mali Dönem Veritabanı",
                           "Mali Dönem Veritabanı Yedek Listesi",
                           $"Mali döneme ait veritabanı için hiç yedek bulunamadı.",
                           "Veritabanı güvenliği için yedek alınması gerekli.");
                    return new SuccessApiDataResponse<int>(
                        data: 0,
                        message: "Silinecek yedek bulunamadı.",
                        resultCount: 0);
                }
                var validBackups = backupList.Where(b => b.IsBackupComleted).ToList();
                var invalidBackups = backupList.Where(b => !b.IsBackupComleted).ToList();
                // Geçersiz backup'ları hemen sil
                if (invalidBackups.Any())
                {
                    await _logService.SistemLogInformationAsync(
                        "Mali Dönem Veritabanı",
                        "Mali Dönem Veritabanı Yedek Listesi",
                        $"Mali Dönem'e ait veritabanı için {invalidBackups.Count} geçersiz yedek.",
                        $"{databaseName}, veritabanı için geçersiz yedekler var. Lütfen sistem yöneticiniz ile iletişime geçiniz!");
                }

                // BUSINESS VALIDATION 3: Geçerli backup sayısı kontrolü
                if (validBackups.Count <= keepLast)
                {
                    var message = validBackups.Count == 0
                        ? "Geçerli backup bulunamadı."
                        : $"Mevcut geçerli backup sayısı ({validBackups.Count}), korunacak sayıdan ({keepLast}) az veya eşit. Silme işlemi yapılmadı.";

                    await _logService.SistemLogInformationAsync(
                       "Mali Dönem Veritabanı",
                       "Mali Dönem Veritabanı Yedek Listesi",
                       $"Yedek detayları işlendi",
                       $"{message}");

                    return new SuccessApiDataResponse<int>(
                        data: 0,
                        message: message,
                        resultCount: validBackups.Count);
                }

                // BUSINESS LOGIC: Silme işlemi öncesi log
                var backupsToDelete = validBackups.Count - keepLast;
                var totalSizeMB = validBackups.Skip(keepLast).Sum(b => b.BackupFileSizeBytes) / (1024 * 1024);
                await _logService.SistemLogInformationAsync("Mali Dönem Veritabanı",
                       "Mali Dönem Veritabanı Yedek Listesi",
                       "Yedekler temizleniyor",
                       $"Toplam geçerli yedek {validBackups.Count}\n," +
                       $"Korunacak: {keepLast}\n+" +
                       $"Silinecek: {backupsToDelete}\n" +
                       $"Tahmini boşalan alan: {totalSizeMB}");

                // Data katmanı metodunu çağır
                var deletedCount = await _backupManager.CleanOldBackupsAsync(databaseName, keepLast, cancellationToken);

                // BUSINESS LOGIC: Sonuç değerlendirme
                if (deletedCount <= 0)
                {
                    await _logService.SistemLogErrorAsync("Mali Dönem Veritabanı",
                      "Mali Dönem Veritabanı Yedek Listesi",
                      "Hiçbir yedek silinemedi",
                      $"Yedekler silinirken bir hata oluştu");
                    return new ErrorApiDataResponse<int>(
                        data: 0,
                        message: "Yedekler silinirken bir hata oluştu.",
                        resultCount: validBackups.Count);
                }

                // Kalan backup'ları tekrar kontrol et
                var remainingBackups = await _backupManager.GetBackupsAsync(databaseName);
                var remainingValidBackups = remainingBackups.Where(b => b.IsBackupComleted).ToList();
                await _logService.SistemLogInformationAsync("Mali Dönem Veritabanı",
                       "Mali Dönem Veritabanı Yedek Listesi",
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
                await _logService.SistemLogInformationAsync(
                  "Mali Dönem Veritabanı Yedek Listesi",
                  "Mali Dönem Veritabanını Yedekleme",
                  "İşlem durduruldu",
                  "Kullanıcı temizleme işlemini durdurdu");
                return new ErrorApiDataResponse<int>(
                    data: -1,
                    message: "İşlem kullanıcı tarafından iptal edildi.",
                    resultCount: 0);
            }
            catch (Exception ex)
            {
                await _logService.SistemLogExceptionAsync("Mali Dönem Veritabanı Yedek Listesi", "Veritabanını Yedekleme", ex);
                return new ErrorApiDataResponse<int>(
                    data: -1,
                    message: "Yedek temizleme sırasında teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }
        public async Task<ApiDataResponse<DatabaseBackupResult>> CreateBackupAsync(string databaseName, DatabaseBackupType backupType, CancellationToken cancellationToken)
        {
            try
            {
                await _logService.SistemLogInformationAsync(
                   "Mali Dönem Veritabanı Yedek Listesi",
                   "Veritabanını Yedekleme",
                   "İşlem başlatılıyor...",
                   $"Yedekleme Tipi: {backupType}");

                var result = await _backupManager.CreateBackupAsync(databaseName, backupType, cancellationToken);

                if (result.IsBackupComleted)
                {
                    await _logService.SistemLogInformationAsync(
                  "Mali Dönem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem başarılı",
                  $"Yedeklenen Veritabanı dosyası : {result.BackupFileName} Boyut: {result.BackupFileSizeBytes}");

                    return new SuccessApiDataResponse<DatabaseBackupResult>(
                        data: result,
                        message: result.Message,
                        resultCount: 1);
                }
                else
                {
                    await _logService.SistemLogErrorAsync(
                  "Mali Dönem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem başarısız",
                  $"Hata açıklaması : {result.Message}");

                    return new ErrorApiDataResponse<DatabaseBackupResult>(
                        data: result,
                        message: result.Message,
                        resultCount: 0);
                }
            }
            catch (OperationCanceledException)
            {
                await _logService.SistemLogInformationAsync(
                  "Mali Dönem Veritabanı Yedek Listesi",
                  "Veritabanını Yedekleme",
                  "İşlem durduruldu",
                  "Kullanıcı yedekleme işlemini durdurdu");
                return new ErrorApiDataResponse<DatabaseBackupResult>(
                    data: null,
                    message: "Yedek alma işlemi iptal edildi.",
                    resultCount: 0);
            }
            catch (Exception ex)
            {
                await _logService.SistemLogExceptionAsync("Mali Dönem Veritabanı Yedek Listesi", "Veritabanını Yedekleme", ex);
                return new ErrorApiDataResponse<DatabaseBackupResult>(
                    data: null,
                    message: "Yedek oluşturulurken teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }
        public async Task<ApiDataResponse<List<DatabaseBackupResult>>> GetBackupHistoryAsync(string databaseName)
        {
            try
            {
                var backupList = new List<DatabaseBackupResult>();

                var backups = await _backupManager.GetBackupsAsync(databaseName);

                if (!backups.Any())
                {
                    return new SuccessApiDataResponse<List<DatabaseBackupResult>>(
                        data: backupList,
                        message: "Henüz Mali Dönem'a ait veritabanının yedeği bulunmuyor.",
                        resultCount: 0);
                }

                var validBackups = backups.Where(b => b.IsBackupComleted).ToList();
                var invalidBackups = backups.Where(b => !b.IsBackupComleted).ToList();

                if (invalidBackups.Any())
                {
                    await _logService.SistemLogInformationAsync(
                        "Mali Dönem Veritabanı Yedek Listesi",
                        "Veritabanı Yedekleme",
                        $"Mali Dönem'e ait veritabanı için {invalidBackups.Count} geçersiz yedek.",
                        "Mali Dönem'e veritabanı için geçersiz yedekler var. Lütfen sistem yöneticiniz ile iletişime geçiniz!");
                }

                await _logService.SistemLogInformationAsync(
                    "Mali Dönem Veritabanı Yedek Listesi",
                    "Veritabanı Yedekleme",
                    $"{validBackups.Count} yedek bulundu. ({validBackups.Count} geçerli, {invalidBackups.Count} geçersiz)",
                    $"Toplam Yedek : {backups.Count}, Geçerli Yedek : {validBackups.Count}, Geçersiz Yedek : {invalidBackups.Count}");

                return new SuccessApiDataResponse<List<DatabaseBackupResult>>(
                    data: backups,
                    message: $"{validBackups.Count} adet geçerli backup bulundu.",
                    resultCount: backups.Count);
            }
            catch (Exception ex)
            {
                await _logService.SistemLogExceptionAsync("Mali Dönem Veritabanı Yedek Listesi", "Veritabanı Yedekleme", ex);
                return new ErrorApiDataResponse<List<DatabaseBackupResult>>(
                    data: null,
                    message: "Veritabanı Yedek geçmişi alınamadı.",
                    resultCount: 0);
            }
        }
        public DateTime? GetLastBackupDate(string databaseName)
        {
            try
            {
                var lastBackupDate = _backupManager.GetLastBackupDate(databaseName);


                return lastBackupDate;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<ApiDataResponse<DatabaseRestoreExecutionResult>> RestoreBackupAsync(string databaseName,
      string backupFileName,
      CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupFileName))
                {
                    return new ErrorApiDataResponse<DatabaseRestoreExecutionResult>(
                        data: null,
                        message: "Yedeklenecek dosya adı belirtilmelidir.",
                        resultCount: 0);
                }

                await _logService.SistemLogInformationAsync(
                    "Mali Dönem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri Yükleme",
                    "İşlem başlatılıyor...",
                    $"Geri yüklenmesi beklenen yedek dosyası: {backupFileName}");


                var result = await _backupManager.RestoreBackupAsync(databaseName, backupFileName, cancellationToken);

                if (result.IsRestoreSuccess)
                {
                    await _logService.SistemLogInformationAsync(
                        "Mali Dönem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "İşlem başarılı",
                        $"Geri yüklenen yedek dosyası: {backupFileName}");

                    return new SuccessApiDataResponse<DatabaseRestoreExecutionResult>(
                        data: result,
                        message: result.Message,
                        resultCount: 1);
                }
                else
                {
                    await _logService.SistemLogErrorAsync(
                        "Mali Dönem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "İşlem başarısız",
                        $"Geri yüklenemeyen yedek dosyası: {backupFileName}");

                    return new ErrorApiDataResponse<DatabaseRestoreExecutionResult>(
                        data: result,
                        message: result.Message ?? "Geri yükleme başarısız oldu.",
                        resultCount: 0);
                }
            }
            catch (Exception ex)
            {
                await _logService.SistemLogExceptionAsync(
                    "Mali Dönem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri yükleme",
                    ex);
                return new ErrorApiDataResponse<DatabaseRestoreExecutionResult>(
                    data: null,
                    message: "Geri yükleme sırasında teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }
        public async Task<ApiDataResponse<bool>> RestoreFromLatestBackupAsync(string databaseName, CancellationToken cancellationToken)
        {
            try
            {
                await _logService.SistemLogInformationAsync(
                    "Mali Dönem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri Yükleme",
                    "⏳ Geri yükleme işlemi başlatılıyor.",
                    "Sistem veritabanı son yedeği ile değiştirilecek!");
                var result = await _backupManager.RestoreFromLatestBackupAsync(databaseName, cancellationToken);
                if (result)
                {
                    await _logService.SistemLogInformationAsync(
                        "Mali Dönem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "✅ Geri yükleme işlemi başarılı.",
                        $"Sistem veritabanı son yedeği ile {DateTime.Now.ToShortDateString} tarihinde değiştirildi");
                    return new SuccessApiDataResponse<bool>(
                        data: result,
                        message: "✅ En son yedekten geri yükleme başarılı");
                }
                else
                {
                    await _logService.SistemLogErrorAsync(
                        "Mali Dönem Veritabanı Yedek Listesi",
                        "Veritabanını Yedekten Geri Yükleme",
                        "❌ Geri yükleme işlemi başarısız.",
                        $"Sistem veritabanı geri yükleme işlemi başarısız oldu!");
                    return new ErrorApiDataResponse<bool>(
                        data: false,
                        message: "❌ Son yedekten geri yükleme işlemi başarısız");
                }
            }
            catch (Exception ex)
            {
                await _logService.SistemLogExceptionAsync(
                    "Mali Dönem Veritabanı Yedek Listesi",
                    "Veritabanını Yedekten Geri yükleme",
                    ex);
                return new ErrorApiDataResponse<bool>(
                    data: false,
                    message: "Geri yükleme sırasında teknik bir hata oluştu.",
                    resultCount: 0);
            }
        }
    }
}
