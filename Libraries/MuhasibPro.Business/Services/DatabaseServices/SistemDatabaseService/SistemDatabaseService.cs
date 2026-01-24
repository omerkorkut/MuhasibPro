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

        public async Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetSistemDatabaseStateAsync()
        {
            var analysis = new DatabaseConnectionAnalysis();
            try
            {
                var analysisResult = await _databaseManager.GetSistemDatabaseStateAsync();
                if (analysisResult == null) 
                {
                    return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis,message:"Sistem veritabanı analiz edilemedi");
                }
                if(analysisResult.HasError)
                {
                    await _logService.SistemLogService.SistemLogErrorAsync(
                        "Sistem Veritabanı İşlemleri", 
                        "Sistem Veritabanı Analizi", 
                        "Analiz işleminde hata oluştu", analysis.Message);
                }
                return new SuccessApiDataResponse<DatabaseConnectionAnalysis>(data:analysisResult,message: analysisResult.Message);
            }
            catch (Exception ex)
            {                
                return new ErrorApiDataResponse<DatabaseConnectionAnalysis>(data: analysis,message: $"[HATA] {ex.Message}");
            }
        }

    

        public async Task<(bool intializeState, string message)> InitializeSistemDatabaseAsync()
        {
            return await _databaseManager.InitializeSistemDatabaseAsync();
        }

        public async Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync()
        {
            return await _databaseManager.ValidateSistemDatabaseAsync();
        }
    }
}
