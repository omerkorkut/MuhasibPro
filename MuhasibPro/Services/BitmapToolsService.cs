using Microsoft.UI.Xaml.Media.Imaging;
using MuhasibPro.Business.Contracts.UIServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace MuhasibPro.Services
{
    public class BitmapToolsService : IBitmapToolsService
    {
        public async Task<object> LoadBitmapAsync(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    var bitmap = new BitmapImage();
                    await stream.WriteAsync(bytes.AsBuffer());
                    stream.Seek(0);
                    await bitmap.SetSourceAsync(stream);
                    return bitmap;
                }
            }
            return null;
        }
        public Lazy<Task<object>> CreateLazyImageLoader(byte[] imageData)
        {
            return new Lazy<Task<object>>(async () =>
            {
                if (imageData == null || imageData.Length == 0)
                    return null;

                return await LoadBitmapAsync(imageData).ConfigureAwait(false);
            });
        }
    }

}
