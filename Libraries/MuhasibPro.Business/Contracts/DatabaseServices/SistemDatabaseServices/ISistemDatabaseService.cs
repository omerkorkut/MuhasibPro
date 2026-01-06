using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices
{
    public interface ISistemDatabaseService
    {
        Task<ApiDataResponse<bool>> InitializeDatabaseAsync();
        //connection methods
        Task<ApiDataResponse<bool>> ValidateConnectionAsync();
        ISistemDatabaseOperationService SistemDatabaseOperation { get; }

    }
}
