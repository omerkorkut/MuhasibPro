using MuhasibPro.Business.DTOModel.SistemModel;
using MuhasibPro.Domain.Common;
using MuhasibPro.Domain.Entities.MuhasebeEntity.DegerlerEntities;
using MuhasibPro.Domain.Enum;

namespace MuhasibPro.Business.Contracts.SistemServices.LogServices
{
    public interface IAppLogService
    {
        Task WriteAsync(LogType type, string source, string action, string message, string description);
        Task WriteAsync(LogType type, string source, string action, Exception ex);
        Task<AppLogModel> GetAppLogAsync(long id);
        Task<IList<AppLogModel>> GetAppLogsAsync(int skip, int take, DataRequest<AppLog> request);
        Task<int> GetAppLogsCountAsync(DataRequest<AppLog> request);
        Task<int> DeleteAppLogAsync(AppLogModel model);
        Task<int> DeleteAppLogRangeAsync(int index, int length, DataRequest<AppLog> request);
        Task AppLogMarkAllAsReadAsync();

    }
}
