namespace MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;

public interface ITransaction : IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
