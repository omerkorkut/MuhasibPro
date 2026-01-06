using MuhasibPro.Domain.Models;

namespace MuhasibPro.Business.Contracts.CommonServices;

public interface IFilePickerService
{
    Task<ImagePickerResult> OpenImagePickerAsync();
}