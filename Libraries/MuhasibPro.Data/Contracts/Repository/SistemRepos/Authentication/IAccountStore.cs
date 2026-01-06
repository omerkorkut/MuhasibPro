using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication
{
    public interface IAccountStore
    {
        Hesap CurrentAccount { get; set; }

        event Action StateChanged;
    }
}
