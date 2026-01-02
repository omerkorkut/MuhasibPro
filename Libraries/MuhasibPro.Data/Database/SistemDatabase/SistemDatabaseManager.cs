using Microsoft.Extensions.Logging;
using MuhasibPro.Data.Concrete.Database.SistemDatabase;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.Extensions;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Domain.Models.DatabaseResultModel;

namespace MuhasibPro.Data.Database.SistemDatabase
{
    public class SistemDatabaseManager : ISistemDatabaseManager
    {        
        private readonly ILogger<SistemDatabaseManager> _logger;
        private readonly ISistemMigrationManager _migrationManager;
        private readonly ISistemBackupManager _backupManager;
        private readonly IApplicationPaths _applicationPaths;
        private const string _databaseName = DatabaseConstants.SISTEM_DB_NAME;
        public SistemDatabaseManager(
            ILogger<SistemDatabaseManager> logger,
            ISistemMigrationManager migrationManager,
            ISistemBackupManager backupManager,
            IApplicationPaths applicationPaths,
            SistemDbContext dbContext)
        {
            _logger = logger;
            _migrationManager = migrationManager;
            _backupManager = backupManager;
            _applicationPaths = applicationPaths;
           
        }      
        private bool _isSistemDatabaseExist => _applicationPaths.SistemDatabaseFileExists();

        public async Task<bool> InitializeSistemDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_isSistemDatabaseExist)
                    return false;

                _logger.LogInformation("Creating new database or update database: {DatabaseName}", _databaseName);

                var intializeDatabase = await _migrationManager.InitializeSistemDatabaseAsync(cancellationToken);

                _logger.LogInformation("Database created or updated successfully: {DatabaseName}", _databaseName);
                return intializeDatabase;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database: {DatabaseName}", _databaseName);
                return false;
            }
        }
        public async Task<DatabaseConnectionAnalysis> GetSistemDatabaseStateAsync(CancellationToken cancellationToken)
        {
            var analysis = new DatabaseConnectionAnalysis();
            try
            {              
                var databaseHealty = await _migrationManager.GetSistemDatabaseStateAsync(cancellationToken);
                if(databaseHealty == null)
                {
                    databaseHealty.HasError = true;
                    databaseHealty.Message = "[Hata] ❌ Veritabanı durum analizi yapılamadı.";
                }
                analysis = databaseHealty;
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı analiz hatası: {DatabaseName}", _databaseName);                
                analysis.HasError = true;
                analysis.Message = "[Hata] ❌ Veritabanı durum analizi yapılamadı.";
                return analysis;
            }
        }
        public async Task<(bool isValid, string Message)> ValidateSistemDatabaseAsync(CancellationToken cancellationToken)
        {
            var result = await GetSistemDatabaseStateAsync(cancellationToken);

            // Eğer result null gelirse sistemin çökmemesi için (Opsiyonel)
            if (result == null)
                return (false, "[Hata] ❌ Veritabanı durum analizi yapılamadı.");

            return result.ToLegacyResult();
        }
   
   
    }


}
