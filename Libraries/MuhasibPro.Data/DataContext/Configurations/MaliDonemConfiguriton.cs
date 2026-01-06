using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.DataContext.Configurations
{

    public class MaliDonemConfiguration : IEntityTypeConfiguration<MaliDonem>
    {
        public void Configure(EntityTypeBuilder<MaliDonem> builder)
        {
            builder.HasOne(m => m.Firma)
                   .WithMany(f => f.MaliDonemler)
                   .HasForeignKey(m => m.FirmaId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
