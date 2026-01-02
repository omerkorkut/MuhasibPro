using Microsoft.EntityFrameworkCore;
using MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities;

namespace MuhasibPro.Data.DataContext
{
    public class AppDbContext : DbContext
    {
        protected AppDbContext()
        {
        }
        public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
       
        public DbSet<TenantDatabaseVersiyon> TenantDatabaseVersiyonlar { get; set; }

        public DbSet<AppLog> AppLogs { get; set; }
       
    }
}
