namespace MuhasibPro.Business.Contracts.UIServices;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);
}
