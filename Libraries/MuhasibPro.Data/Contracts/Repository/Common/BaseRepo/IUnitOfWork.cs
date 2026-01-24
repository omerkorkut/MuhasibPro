namespace MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;

public interface IUnitOfWork<TContext> : IDisposable where TContext : class
{
    Task<ITransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync();
    TContext Context { get; }
}
