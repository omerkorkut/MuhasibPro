using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Domain.Enum;

namespace MuhasibPro.Business.Contracts.SistemServices.LogServices
{
    public interface ISistemLogService
    {
        Task WriteAsync(LogType type, string source, string action, string message, string description);
        Task WriteAsync(LogType type, string source, string action, Exception ex);
        Task<SistemLogModel> GetSistemLogAsync(long id);
        Task<IList<SistemLogModel>> GetSistemLogsAsync(int skip, int take, DataRequest<SistemLog> request);
        Task<int> GetSistemLogsCountAsync(DataRequest<SistemLog> request);
        Task<int> DeleteSistemLogAsync(SistemLogModel model);
        Task<int> DeleteSistemLogRangeAsync(int index, int length, DataRequest<SistemLog> request);
        Task SistemLogMarkAllAsReadAsync();
    }
}
