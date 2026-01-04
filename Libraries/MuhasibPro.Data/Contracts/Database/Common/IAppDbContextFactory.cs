using MuhasibPro.Data.DataContext;

namespace MuhasibPro.Data.Contracts.Database.Common
{
    public interface IAppDbContextFactory
    {
        AppDbContext CreateDbContext(string databaseName);
        Task<(bool canConnect, string message)> TestDbContextConnectionAsync(string databaseName, CancellationToken cancellationToken = default);
    }
}
