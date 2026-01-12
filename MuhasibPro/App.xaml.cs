using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MuhasibPro.Business.Contracts.CommonServices;
using MuhasibPro.Business.Contracts.DatabaseServices.SistemDatabaseServices;
using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Business.Contracts.UIServices;
using MuhasibPro.Business.DTOModel;
using MuhasibPro.Business.Services.DatabaseServices.SistemDatabaseService;
using MuhasibPro.Business.Services.SistemServices;
using MuhasibPro.Business.Services.SistemServices.Authetication;
using MuhasibPro.Business.Services.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Database.Common;
using MuhasibPro.Data.Contracts.Database.Common.Helpers;
using MuhasibPro.Data.Contracts.Database.SistemDatabase;
using MuhasibPro.Data.Contracts.Database.TenantDatabase;
using MuhasibPro.Data.Contracts.Repository.Common;
using MuhasibPro.Data.Contracts.Repository.Common.BaseRepo;
using MuhasibPro.Data.Contracts.Repository.SistemRepos;
using MuhasibPro.Data.Contracts.Repository.SistemRepos.Authentication;
using MuhasibPro.Data.Database.Common;
using MuhasibPro.Data.Database.Common.Helpers;
using MuhasibPro.Data.Database.SistemDatabase;
using MuhasibPro.Data.Database.TenantDatabase;
using MuhasibPro.Data.DataContext;
using MuhasibPro.Data.Repository.Common;
using MuhasibPro.Data.Repository.Common.BaseRepo;
using MuhasibPro.Data.Repository.SistemRepos;
using MuhasibPro.Data.Repository.SistemRepos.Authentication;
using MuhasibPro.Domain.Entities.SistemEntity;
using MuhasibPro.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MuhasibPro
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public static IServiceProvider Services { get; private set; }

        // Service collection'ı tutmak için
        private static ServiceCollection _serviceCollection;
        private static ILoggerFactory _loggerFactory;

        public App()
        {
            this.InitializeComponent();

            // Service collection oluştur
            _serviceCollection = new ServiceCollection();

            // ViewModel'ları ve servisleri kaydet
            ConfigureServices(_serviceCollection);

            // Service provider'ı oluştur
            Services = _serviceCollection.BuildServiceProvider();

            // Community Toolkit'in Ioc provider'ını ayarla
            Ioc.Default.ConfigureServices(Services);
        }

        private void ConfigureServices(ServiceCollection services)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSingleton(_loggerFactory);
            services.AddSingleton<IApplicationPaths, ApplicationPaths>();
            services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();

            services.AddSingleton<ISistemLogRepository, SistemLogRepository>();
            services.AddSingleton<IAppLogRepository, AppLogRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IAuthenticationRepository, AuthenticationRepository>();
            services.AddSingleton<IAccountStore, AccountStore>();
            services.AddSingleton<IAuthenticator, Authenticator>();
            services.AddSingleton<IFirmaRepository, FirmaRepository>();
            services.AddSingleton<IMaliDonemRepository, MaliDonemRepository>();
            
            services.AddScoped<IUnitOfWork<SistemDbContext>, UnitOfWork<SistemDbContext>>();
            services.AddScoped<IUnitOfWork<AppDbContext>, UnitOfWork<AppDbContext>>();
            services.AddSingleton<IPasswordHasher<Hesap>>(provider => new PasswordHasher<Hesap>());

            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton<IDatabaseBackupManager, DatabaseBackupManager>();
            services.AddSingleton<ISistemBackupManager, SistemBackupManager>();
            services.AddSingleton<ISistemDatabaseManager, SistemDatabaseManager>();
            services.AddSingleton<ISistemMigrationManager, SistemMigrationManager>();
            services.AddSingleton<ITenantSQLiteConnectionStringFactory, TenantSQLiteConnectionStringFactory>();

            services.AddSingleton<IBitmapToolsService, BitmapToolsService>();
            services.AddSingleton<ISistemLogService, SistemLogService>();
            services.AddSingleton<IAppLogService, AppLogService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<ISistemDatabaseOperationService, SistemDatabaseOperationService>();
            services.AddSingleton<ISistemDatabaseService, SistemDatabaseService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IMessageService,MessageService>();
            services.AddSingleton<ModelFactory>();
            services.AddSingleton<IAppDbContextFactory, AppDbContextFactory>();

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
                                   var migrationAssemblies = typeof(SistemMigrationManager).Assembly;
                                   sqliteOptions.MigrationsAssembly(migrationAssemblies.FullName);
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
                           

                           // Yoksa sistem context'i
                           return factory.CreateDbContext("Sistem");
                       });

        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
