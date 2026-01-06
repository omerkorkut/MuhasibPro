using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication
{
    public interface IUserRepository : IRepository<Hesap>
    {
        Task<Hesap?> GetByEmailAsync(string email);
        Task<Hesap?> GetByUsernameAsync(string userName);


    }
}
