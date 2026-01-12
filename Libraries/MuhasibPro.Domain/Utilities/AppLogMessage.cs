namespace MuhasibPro.Domain.Utilities
{
    public static class AppLogMessage
    {      

        #region Log Durumları
        public static class LogDurum
        {
            public const string Hata = "HATA";
            public const string Bilgi = "BİLGİ";
            public const string Dikkat = "DİKKAT";
            public const string Uyari = "UYARI";
            public const string Basarili = "BAŞARILI";
            public const string Kritik = "KRİTİK";
        }
        public static class LogSourceVeritabani
        {
            public const string VeritabaniYedektenGeriYuklemeIslemi = "🔁 Veritabanı Yedekten Geri Yükleme İşlemi";
            public const string VeritabaniYedeklemeIslemi = "🗃️ Veritabanı Yedekleme İşlemi";
            
        }
        public static class LogMessageVeritabani
        {
            public const string VeritabaniGeriYuklemeBASARILI = "✅ Veritabanı başarılı bir şekilde geri yüklendi";
            public const string VeritabaniGeriYuklemeHATA = "❌ Veritabanı geri yükleme işlemi başarısız oldu!";
            
            public const string VeritabaniYedekleBASARILI = "✅ Veritabanı başarılı bir şekilde yedeklendi";
            public const string VeritabaniYedekleHATA = "❌ Veritabanı yedekleme işlemi başarısız!";
        }
        #endregion

      
    }
}