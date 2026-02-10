using Microsoft.EntityFrameworkCore;
using MuhasibPro.Domain.Entities.MuhasebeEntity.Senet;

namespace MuhasibPro.Data.DataContext.SeedData;

public static class SeedDataSenet
{
    public static void SenetMahkemeSec(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SenetMahkemeler>()
            .HasData(
                new SenetMahkemeler { Id = 1, Mahkeme = "İstanbul" },
                new SenetMahkemeler { Id = 2, Mahkeme = "Ankara" },
                new SenetMahkemeler { Id = 3, Mahkeme = "İzmir" },
                new SenetMahkemeler { Id = 4, Mahkeme = "Adana" }

            );
    }
}
