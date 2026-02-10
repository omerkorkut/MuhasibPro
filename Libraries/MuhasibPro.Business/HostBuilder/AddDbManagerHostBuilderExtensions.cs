using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

namespace MuhasibPro.Business.HostBuilder
{
    public static class AddDbManagerHostBuilderExtensions
    {
        public static IHostBuilder AddDatabaseManagement(this IHostBuilder host)
        {
            host.ConfigureServices(
                (context, services) =>
                {
                    services.AddDbContext<SistemDbContext>(
                        (provider, options) =>
                        {
                            // ⭐ DOĞRUSU: IApplicationPaths'den METOD çağır
                            var appPaths = provider.GetRequiredService<IApplicationPaths>();
                            var sistemDbPath = appPaths.GetSistemDatabaseFilePath();

                            var connectionString = $"Data Source={sistemDbPath};Mode=ReadWriteCreate;";


                            options.UseSqlite(
                                connectionString,
                                sqliteOptions =>
                                {
                                    sqliteOptions.CommandTimeout(30);                                    
                                });

#if DEBUG
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();

#endif
                        });


                    services.AddScoped<AppDbContext>(
                        provider =>
                        {
                            var factory = provider.GetRequiredService<IAppDbContextFactory>();
                            var tenantManager = provider.GetService<ITenantSQLiteSelectionManager>();
                            var currentTenant = tenantManager.GetCurrentTenant();

                            // Eğer aktif tenant varsa onun context'ini ver
                            if (tenantManager?.IsTenantLoaded == true)
                            {
                                return factory.CreateDbContext(currentTenant.DatabaseName);
                            }
                            return factory.CreateDbContext("MuhasibPro");
                        });

                    services.AddScoped<ILocalUpdateManager, LocalUpdateManager>();
                    //Sistem Database Managers
                    services.AddSingleton<ISistemDatabaseManager, SistemDatabaseManager>();
                    services.AddSingleton<ISistemBackupManager, SistemBackupManager>();
                    services.AddSingleton<ISistemMigrationManager, SistemMigrationManager>();

                    // Tenant Managers

                    services.AddSingleton<IAppDbContextFactory, AppDbContextFactory>();
                    services.AddSingleton<IDatabaseBackupManager, DatabaseBackupManager>();

                    services.AddSingleton<ITenantSQLiteConnectionStringFactory, TenantSQLiteConnectionStringFactory>();
                    services.AddSingleton<ITenantSQLiteBackupManager, TenantSQLiteBackupManager>();
                    services.AddSingleton<ITenantSQLiteDatabaseManager, TenantSQLiteDatabaseManager>();
                    services.AddSingleton<ITenantSQLiteMigrationManager, TenantSQLiteMigrationManager>();
                    services.AddSingleton<ITenantSQLiteSelectionManager, TenantSQLiteSelectionManager>();


                    //Sistem Database Service
                    services.AddSingleton<ISistemDatabaseService, SistemDatabaseService>();
                    services.AddSingleton<ISistemDatabaseOperationService, SistemDatabaseOperationService>();

                    //Tenant Database Services                    

                    
                    services.AddSingleton<ITenantSQLiteDatabaseLifecycleService, TenantSQLiteDatabaseLifecycleService>();
                    services.AddSingleton<ITenantSQLiteDatabaseOperationService, TenantSQLiteDatabaseOperationService>();
                    services.AddSingleton<ITenantSQLiteDatabaseSelectedDetailService, TenantSQLiteDatabaseSelectedDetailService>();
                    services.AddSingleton<ITenantSQLiteSelectionService, TenantSQLiteSelectionService>();
                    services.AddSingleton<ITenantSQLiteDatabaseService, TenantSQLiteDatabaseService>();



                    //Database Infrastructure Services
                    services.AddSingleton<IMaliDonemSagaStep, MaliDonemSagaStep>();
                    services.AddSingleton<ITenantDatabaseSagaStep, TenantDatabaseSagaStep>();
                    services.AddSingleton<ITenantBackupService, TenantBackupService>();
                    services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
                    services.AddSingleton<IApplicationPaths, ApplicationPaths>();

                });
            return host;
        }
    }
}
