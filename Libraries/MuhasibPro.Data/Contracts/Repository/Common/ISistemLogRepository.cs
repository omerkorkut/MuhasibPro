using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace MuhasibPro.Data.Contracts.Repository.Common
{
    public interface ISistemLogRepository : IRepository<SistemLog>
    {
        Task<SistemLog> GetLogAsync(long id);
        Task<IList<SistemLog>> GetLogsAsync(int skip, int take, DataRequest<SistemLog> request);
        Task<IList<SistemLog>> GetLogKeysAsync(int skip, int take, DataRequest<SistemLog> request);
        Task<int> GetLogsCountAsync(DataRequest<SistemLog> request);
        Task<int> CreateLogAsync(SistemLog appLog);
        Task<int> DeleteLogsAsync(params SistemLog[] logs);
        Task MarkAllAsReadAsync();
    }
}
