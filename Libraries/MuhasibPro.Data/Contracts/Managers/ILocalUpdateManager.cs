using MuhasibPro.Domain.Models;

namespace MuhasibPro.Data.Contracts.Common
{
    public interface ILocalUpdateManager
    {
        public UpdateSettingsModel UpdateSettings { get; set; }
        public Task<UpdateSettingsModel> LoadAsync();
        public Task SaveAsync(UpdateSettingsModel model);
    }
}
