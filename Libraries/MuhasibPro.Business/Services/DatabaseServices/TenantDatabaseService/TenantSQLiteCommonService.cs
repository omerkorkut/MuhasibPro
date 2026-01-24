using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices;
using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common;

namespace MuhasibPro.Business.Services.DatabaseServices.TenantDatabaseService
{
    public class TenantSQLiteCommonService : ITenantSQLiteCommonService
    {
        public TenantSQLiteCommonService(
            ITenantSQLiteDatabaseLifecycleService tenantSQLiteDatabaseLifecycleService,
            ITenantSQLiteDatabaseOperationService tenantSQLiteDatabaseOperationService,
            ITenantSQLiteDatabaseSelectedDetailService tenantSQLiteDatabaseSelectedDetailService,
            ITenantSQLiteDatabaseService tenantSQLiteDatabaseService,
            ITenantSQLiteSelectionService tenantSQLiteSelectionService,
            IMaliDonemSagaStep maliDonemSagaStep,
            ITenantBackupService tenantBackupService,
            ITenantDatabaseSagaStep tenantDatabaseSagaStep)
        {
            TenantSQLiteDatabaseLifecycleService = tenantSQLiteDatabaseLifecycleService;
            TenantSQLiteDatabaseOperationService = tenantSQLiteDatabaseOperationService;
            TenantSQLiteDatabaseSelectedDetailService = tenantSQLiteDatabaseSelectedDetailService;
            TenantSQLiteDatabaseService = tenantSQLiteDatabaseService;
            TenantSQLiteSelectionService = tenantSQLiteSelectionService;
            MaliDonemSagaStep = maliDonemSagaStep;
            TenantBackupService = tenantBackupService;
            TenantDatabaseSagaStep = tenantDatabaseSagaStep;
        }

        public ITenantSQLiteDatabaseLifecycleService TenantSQLiteDatabaseLifecycleService { get; }

        public ITenantSQLiteDatabaseOperationService TenantSQLiteDatabaseOperationService { get; }

        public ITenantSQLiteDatabaseSelectedDetailService TenantSQLiteDatabaseSelectedDetailService { get; }

        public ITenantSQLiteDatabaseService TenantSQLiteDatabaseService { get; }

        public ITenantSQLiteSelectionService TenantSQLiteSelectionService { get; }

        public IMaliDonemSagaStep MaliDonemSagaStep { get; }

        public ITenantBackupService TenantBackupService { get; }

        public ITenantDatabaseSagaStep TenantDatabaseSagaStep { get; }
    }
}
