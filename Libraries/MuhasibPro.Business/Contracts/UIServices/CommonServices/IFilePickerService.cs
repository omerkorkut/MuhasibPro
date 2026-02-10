using MuhasibPro.Domain.Models;

namespace MuhasibPro.Business.Contracts.UIServices.CommonServices;

public interface IFilePickerService
{
    Task<ImagePickerResult> OpenImagePickerAsync();
}