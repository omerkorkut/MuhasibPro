using Microsoft.EntityFrameworkCore;
using MuhasibPro.Data.DataContext.Configurations;
using MuhasibPro.Data.DataContext.SeedDataSistem;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.DataContext
{
    public class SistemDbContext : DbContext
    {
        protected SistemDbContext()
        {
        }
        public SistemDbContext(DbContextOptions<SistemDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SeedUser(modelBuilder);
            SeedInitialVersion(modelBuilder);
            

            modelBuilder.ApplyConfiguration(new MaliDonemConfiguration());
            modelBuilder.ApplyConfiguration(new KullanicilarConfiguration());

            modelBuilder.Entity<Hesap>()
                .HasKey(h => h.KullaniciId);
        }
        private void SeedUser(ModelBuilder modelBuilder)
        {            
            var yonetici = new Kullanici
            {
                Id = 5413300800,
                Adi = "Ömer",
                AktifMi = true,
                Eposta = "korkutomer@gmail.com",
                KaydedenId = 5413300800,
                KayitTarihi = new DateTime(2025, 03, 12),
                RolId = 241341,
                KullaniciAdi = "korkutomer",
                ParolaHash = "AQAAAAIAAYagAAAAECnYdlrjFiWFJc+FGeGDmvR87uz20oU/Z0K4JE9ddoF2VUnmHw0idEFX8UPOb4cpzQ==",
                Soyadi = "Korkut",
                Telefon = "0 (541) 330 0800",
                ArananTerim = "korkutomer, Ömer Korkut, Yönetici"
            };
            modelBuilder.Entity<Kullanici>().HasData(yonetici);
            SeedDataKullaniciRol.SeedKullaniciRoller(modelBuilder);

        }
        private void SeedInitialVersion(ModelBuilder modelBuilder)
        {
            // SistemDbVersiyon için seed
            var initialSistemDbVersion = new AppDbVersion
            {
                DatabaseName = "Sistem.db",
                CurrentAppVersion = "1.0.0", // Base class property
                CurrentAppVersionLastUpdate = new DateTime(2025, 09, 22),
                PreviousAppVersiyon = null,
                CurrentDatabaseVersion = "1.0.0",
                CurrentDatabaseLastUpdate = new DateTime(2025, 09, 22),
                PreviousDatabaseVersion = null
            };
            modelBuilder.Entity<AppDbVersion>().HasData(initialSistemDbVersion);
        }
        public DbSet<SistemLog> SistemLogs { get; set; }
        public DbSet<AppVersion> AppVersiyonlar { get; set; }
        public DbSet<AppDbVersion> AppDbVersiyonlar { get; set; }
        public DbSet<Hesap> Hesaplar { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<KullaniciRol> KullaniciRoller { get; set; }
        public DbSet<Firma> Firmalar { get; set; }
        public DbSet<MaliDonem> MaliDonemler { get; set; }

    }
}
