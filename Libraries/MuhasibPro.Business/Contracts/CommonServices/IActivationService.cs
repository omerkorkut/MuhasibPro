namespace MuhasibPro.Business.Contracts.CommonServices;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);

}
