using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication
{
    public interface IUserRepository : IRepository<Kullanici>
    {
        Task<Kullanici?> GetByEmailAsync(string email);
        Task<Kullanici?> GetByUsernameAsync(string userName);


    }
}
