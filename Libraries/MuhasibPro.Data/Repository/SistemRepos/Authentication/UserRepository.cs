using Microsoft.EntityFrameworkCore;
using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Data.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Repository.SistemRepos.Authentication
{
    public class UserRepository : BaseRepository<SistemDbContext, Hesap>, IUserRepository
    {
        public UserRepository(SistemDbContext context) : base(context)
        {
        }

        public async Task<Hesap?> GetByEmailAsync(string email)
        {
            if (email == null)
                throw new ArgumentNullException("email");
            return await DbSet
                .Include(a => a.Kullanici)
                .Include(a => a.Kullanici.Rol)
                .FirstOrDefaultAsync(u => u.Kullanici.Eposta == email)
                .ConfigureAwait(false);
        }

        public async Task<Hesap?> GetByUsernameAsync(string userName)
        {
            if (userName == null)
                throw new ArgumentNullException("userName");
            return await DbSet
                .Include(a => a.Kullanici)
                .Include(a => a.Kullanici.Rol)
                .FirstOrDefaultAsync(u => u.Kullanici.KullaniciAdi == userName)
                .ConfigureAwait(false);
        }
    }
}
