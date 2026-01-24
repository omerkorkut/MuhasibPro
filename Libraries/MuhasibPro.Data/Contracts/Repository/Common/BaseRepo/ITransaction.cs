namespace MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;

public interface ITransaction : IDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
