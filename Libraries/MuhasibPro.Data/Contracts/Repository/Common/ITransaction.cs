namespace MuhasibPro.Data.Contracts.Repository.Common;

public interface ITransaction : IDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
