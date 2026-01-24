using Microsoft.Extensions.Logging;

namespace MuhasibPro.Business.ResultModels.TenantResultModels
{
    public class TenantOperationSaga
    {
        private readonly ILogger _logger;
        private readonly List<SagaStep> _executedSteps = new List<SagaStep>();
        private readonly object _lock = new object();

        public TenantOperationSaga(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<T> ExecuteStepAsync<T>(
       string stepName,
       Func<Task<T>> action,
       Func<T, Task> compensate)
        {
            T result = default;
            SagaStep sagaStep = null;

            try
            {
                _logger.LogInformation("Saga Step başlatıldı: {StepName}", stepName);

                // ⭐ Başarılı olursa kullanılacak step'i önceden oluştur
                sagaStep = new SagaStep
                {
                    StepName = stepName
                };

                result = await action();

                // ⭐ Compensate'i BAŞARILI OLDUKTAN SONRA oluştur
                // (result artık biliniyor)
                if (compensate != null)
                {
                    sagaStep.CompensateAction = async () =>
                        await compensate(result);
                }

                sagaStep.Result = result;

                // ✅ Step başarılı olduğunda kaydet
                lock (_lock)
                {
                    _executedSteps.Add(sagaStep);
                }

                _logger.LogInformation("Saga Step tamamlandı: {StepName}", stepName);
                return result;
            }
          
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga Step başarısız: {StepName}", stepName);

                // ⭐ STEP BAŞARISIZ OLDU, COMPENSATE EKLEME
                // (zaten hiç execute edilmedi)
                throw;
            }
        }

        public async Task CompensateAllAsync()
        {
            List<SagaStep> stepsToCompensate;

            lock (_lock)
            {
                if (!_executedSteps.Any())
                {
                    _logger.LogInformation("Rollback edilecek step yok");
                    return;
                }

                stepsToCompensate = new List<SagaStep>(_executedSteps);
                _executedSteps.Clear(); // ⭐ Temizle, tekrar çağrılmaması için
            }

            _logger.LogWarning(
                "Rollback başlatılıyor: {Count} adım",
                stepsToCompensate.Count);

            int totalSteps = stepsToCompensate.Count;
            int completed = 0;

            // Ters sırada rollback (LIFO)
            for (int i = totalSteps - 1; i >= 0; i--)
            {
                var step = stepsToCompensate[i];
                completed++;

                if (step.CompensateAction == null)
                {
                    _logger.LogDebug(
                        "[{Completed}/{Total}] Compensate yok: {StepName}",
                        completed, totalSteps, step.StepName);
                    continue;
                }

                try
                {
                    

                    _logger.LogInformation(
                        "[{Completed}/{Total}] Compensating: {StepName}",
                        completed, totalSteps, step.StepName);

                    await step.CompensateAction();

                    _logger.LogInformation(
                        "[{Completed}/{Total}] Compensated: {StepName}",
                        completed, totalSteps, step.StepName);
                }                
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[{Completed}/{Total}] Compensation başarısız: {StepName}",
                        completed, totalSteps, step.StepName);

                    // ⭐ DEVAM ET: Diğer step'leri compensate etmeye devam
                }
            }

            _logger.LogInformation(
                "Rollback tamamlandı: {Completed}/{Total} adım",
                completed, totalSteps);
        }

        private class SagaStep
        {
            public string StepName { get; set; }
            public object Result { get; set; }
            public Func<Task> CompensateAction { get; set; }
        }
    }

}
