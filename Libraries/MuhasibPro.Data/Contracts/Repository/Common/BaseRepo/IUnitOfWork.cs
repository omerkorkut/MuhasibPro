namespace MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;

public interface IUnitOfWork<TContext> : IDisposable where TContext : class
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    TContext Context { get; }
}
