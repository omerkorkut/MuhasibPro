namespace MuhasibPro.Business.Contracts.UIServices
{
    public interface IBitmapToolsService
    {
        Task<object> LoadBitmapAsync(byte[] bytes);
        Lazy<Task<object>> CreateLazyImageLoader(byte[] imageData);
    }
}
