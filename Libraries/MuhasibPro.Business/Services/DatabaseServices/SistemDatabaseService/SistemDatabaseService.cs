using MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.SistemDatabaseService
{
    public class SistemDatabaseService : ISistemDatabaseService
    {
        private readonly ISistemDatabaseManager _databaseManager;
        
        private readonly ILogService _logService;

        public SistemDatabaseService(ISistemDatabaseManager databaseManager, ILogService logService)
        {
            _databaseManager = databaseManager;
            _logService = logService;
        }

        public async Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetHealthStatusAsync(CancellationToken cancellationToken=default)
        {
            var analysis = new DatabaseConnectionAnalysis();
            try
            {
                var analysisResult = await _databaseManager.GetSistemDatabaseStateAsync(cancellationToken);
                if (analysisResult == null) 
                {
                    return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis,message:"Sistem veritabanı analiz edilemedi");
                }
                if(analysisResult.IsDatabaseExists && analysis.CanConnect)
                {
                    await _logService.SistemLogService.SistemLogInformationAsync("Sistem Veritabanı", "Sistem Veritabanı Analiz İşlemleri", "Analiz işlemi sonuçlandı", analysis.Message);
                }
                return new SuccessApiDataResponse<DatabaseConnectionAnalysis>(data:analysisResult,message: analysisResult.Message);
            }
            catch (Exception ex)
            {                
                return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis,message: $"[HATA] {ex.Message}");
            }
        }

    

        public async Task<(bool intializeState, string message)> InitializeSistemDatabaseAsync(CancellationToken cancellationToken=default)
        {
            return await _databaseManager.InitializeSistemDatabaseAsync(cancellationToken);
        }

        public async Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync(CancellationToken cancellationToken=default)
        {
            return await _databaseManager.ValidateSistemDatabaseAsync(cancellationToken);
        }
    }
}
