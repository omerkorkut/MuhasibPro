using MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Domain.Models.DatabaseResultModel;
using MuhasibPro.Domain.Utilities.Responses;

namespace MuhasibPro.Business.Services.DatabaseServices.SistemDatabaseService
{
    public class SistemDatabaseService : ISistemDatabaseService
    {
        private readonly ISistemMigrationManager _migrationManager;        
        private readonly ILogService _logService;

        public SistemDatabaseService(ILogService logService, ISistemMigrationManager migrationManager)
        {
            _logService = logService;
            _migrationManager = migrationManager;
        }

        public async Task<ApiDataResponse<DatabaseConnectionAnalysis>> GetSistemDatabaseStateAsync()
        {
            var analysis = new DatabaseConnectionAnalysis();
            try
            {
                var analysisResult = await _migrationManager.GetSistemDatabaseStateAsync();
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

    

        public async Task<(bool initializeState, string message)> InitializeSistemDatabaseAsync()
        =>   await _migrationManager.InitializeSistemDatabaseAsync();
        

        public async Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync()
        {
            var result = await GetSistemDatabaseStateAsync();
            if(!result.Success)
                return (false, $"Sistem veritabanı durumunu alırken hata oluştu: {result.Message}");
            return result.Data.ToLegacyResult();
        }
    }
}
