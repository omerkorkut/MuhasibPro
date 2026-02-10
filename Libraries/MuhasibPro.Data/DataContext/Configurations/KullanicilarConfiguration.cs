using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.DataContext.Configurations
{
    public class KullanicilarConfiguration : IEntityTypeConfiguration<Kullanici>
    {
        public void Configure(EntityTypeBuilder<Kullanici> builder)
        {
            builder.HasOne(k => k.Rol)
                   .WithMany()
                   .HasForeignKey(k => k.RolId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
