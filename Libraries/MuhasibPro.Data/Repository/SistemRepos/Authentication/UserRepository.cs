using Microsoft.EntityFrameworkCore;
using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Data.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Repository.SistemRepos.Authentication
{
    public class UserRepository : BaseRepository<SistemDbContext, Kullanici>, IUserRepository
    {
        public UserRepository(SistemDbContext context) : base(context)
        {
        }

        public async Task<Kullanici?> GetByEmailAsync(string email)
        {
            if (email == null)
                throw new ArgumentNullException("email");
            return await DbSet                
                .Include(a => a.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Eposta == email)                
                .ConfigureAwait(false);
        }

        public async Task<Kullanici?> GetByUsernameAsync(string userName)
        {
            if (userName == null)
                throw new ArgumentNullException("userName");
            return await DbSet                
                .Include(a => a.Rol)
                .FirstOrDefaultAsync(u => u.KullaniciAdi == userName)
                .ConfigureAwait(false);
        }
    }
}
