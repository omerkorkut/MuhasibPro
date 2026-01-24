using MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices.Common;

namespace MuhasibPro.Business.Contracts.DatabaseServices.TenantDatabaseServices
{
    public interface ITenantSQLiteCommonService
    {
        ITenantSQLiteDatabaseLifecycleService TenantSQLiteDatabaseLifecycleService { get; }
        ITenantSQLiteDatabaseOperationService TenantSQLiteDatabaseOperationService { get; }
        ITenantSQLiteDatabaseSelectedDetailService TenantSQLiteDatabaseSelectedDetailService { get; }
        ITenantSQLiteDatabaseService TenantSQLiteDatabaseService { get; }
        ITenantSQLiteSelectionService TenantSQLiteSelectionService { get; }
        IMaliDonemSagaStep MaliDonemSagaStep { get; }
        ITenantBackupService TenantBackupService { get; }
        ITenantDatabaseSagaStep TenantDatabaseSagaStep { get; }
    }
}
