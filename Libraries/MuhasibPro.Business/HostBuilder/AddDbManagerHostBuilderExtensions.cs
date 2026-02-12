using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common;
using MuhasibPro.Business.Services.DatabaseServices.SistemDatabaseService;
using MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService;
using MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService.Common;
using MuhasibPro.Data.Contracts.Common;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Database.Common;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.SistemDatabase;
using MuhasibPro.Data.Database.TenantDatabase;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Data.Managers;

public static class AddDbManagerHostBuilderExtensions
{
    public static IHostBuilder AddDatabaseManagement(this IHostBuilder host)
    {
        host.ConfigureServices(
            (context, services) =>
            {
                // ========== 1. SEVİYE: TEMEL ALTYAPI (HİÇBİR BAĞIMLILIĞI YOK) ==========
                services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
                services.AddSingleton<IApplicationPaths, ApplicationPaths>();

                // ========== 2. SEVİYE: SİSTEM VERİTABANI (SADECE TEMEL ALTYAPI BAĞIMLILIĞI) ==========
                services.AddDbContext<SistemDbContext>((provider, options) =>
                {
                    var appPaths = provider.GetRequiredService<IApplicationPaths>();
                    var sistemDbPath = appPaths.GetSistemDatabaseFilePath();
                    var connectionString = $"Data Source={sistemDbPath};Mode=ReadWriteCreate;";

                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.CommandTimeout(30);
                    });

#if DEBUG
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
#endif
                });

                // ========== 3. SEVİYE: SİSTEM MANAGER'LAR (SİSTEM DB BAĞIMLILIĞI) ==========
                
                services.AddSingleton<ISistemBackupManager, SistemBackupManager>();
                services.AddSingleton<ISistemMigrationManager, SistemMigrationManager>();

                // ========== 4. SEVİYE: SİSTEM SERVİSLERİ ==========
                services.AddSingleton<ISistemDatabaseService, SistemDatabaseService>();
                services.AddSingleton<ISistemDatabaseOperationService, SistemDatabaseOperationService>();

                // ========== 5. SEVİYE: TENANT ALTYAPISI (BAĞIMSIZ) ==========
                services.AddSingleton<ITenantSQLiteConnectionStringFactory, TenantSQLiteConnectionStringFactory>();

                // ========== 6. SEVİYE: LOCAL UPDATE MANAGER ==========
                services.AddSingleton<ILocalUpdateManager, LocalUpdateManager>();
                // ========== 7. SEVİYE: DB CONTEXT FACTORY (SELECTION MANAGER BAĞIMLILIĞI) ==========
                services.AddSingleton<IAppDbContextFactory, AppDbContextFactory>();
                services.AddSingleton<IDatabaseBackupManager, DatabaseBackupManager>();
                // ========== 8. SEVİYE: TENANT MANAGER'LAR ==========
                services.AddSingleton<ITenantSQLiteSelectionManager, TenantSQLiteSelectionManager>();
                services.AddSingleton<ITenantSQLiteDatabaseManager, TenantSQLiteDatabaseManager>();
                services.AddSingleton<ITenantSQLiteBackupManager, TenantSQLiteBackupManager>();
                services.AddSingleton<ITenantSQLiteMigrationManager, TenantSQLiteMigrationManager>();

              

                // ========== 9. SEVİYE: TENANT SERVİSLERİ ==========
                services.AddSingleton<ITenantSQLiteDatabaseLifecycleService, TenantSQLiteDatabaseLifecycleService>();
                services.AddSingleton<ITenantSQLiteDatabaseOperationService, TenantSQLiteDatabaseOperationService>();
                services.AddSingleton<ITenantSQLiteDatabaseSelectedDetailService, TenantSQLiteDatabaseSelectedDetailService>();
                services.AddSingleton<ITenantSQLiteSelectionService, TenantSQLiteSelectionService>();
                services.AddSingleton<ITenantSQLiteDatabaseService, TenantSQLiteDatabaseService>();

                // ========== 10. SEVİYE: SAGA STEP'LERİ ==========
                services.AddSingleton<IMaliDonemSagaStep, MaliDonemSagaStep>();
                services.AddSingleton<ITenantDatabaseSagaStep, TenantDatabaseSagaStep>();
                services.AddSingleton<ITenantBackupService, TenantBackupService>();

                // ========== 11. SEVİYE: APPDBCONTEXT (EN SON, EN ÇOK BAĞIMLILIĞI OLAN) ==========
                services.AddScoped<AppDbContext>(provider =>
                {
                    var factory = provider.GetRequiredService<IAppDbContextFactory>();
                    var tenantManager = provider.GetRequiredService<ITenantSQLiteSelectionManager>();

                    // Eğer aktif tenant varsa onun context'ini ver
                    if (tenantManager?.IsTenantLoaded == true)
                    {
                        var currentTenant = tenantManager.GetCurrentTenant();
                        return factory.CreateDbContext(currentTenant.DatabaseName);
                    }

                    // Yoksa sistem context'i
                    return factory.CreateDbContext("Sistem");
                });
            });

        return host;
    }
}