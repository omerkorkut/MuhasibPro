using MuhasibPro.Data.Contracts.Common;
using MuhasibPro.Domain.Models;
using System.Text.Json;

namespace MuhasibPro.Data.Managers
{
    public class LocalUpdateManager : ILocalUpdateManager
    {
        public string SettingsPath { get; private set; }
        public UpdateSettingsModel UpdateSettings { get; set; }

        public LocalUpdateManager()
        {
            var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localFolder, "MuhasibPro");
            Directory.CreateDirectory(appFolder);
            SettingsPath = Path.Combine(appFolder, "settings.json");
        }

        public async Task<UpdateSettingsModel> LoadAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading settings from: {SettingsPath}");
                System.Diagnostics.Debug.WriteLine($"File exists: {File.Exists(SettingsPath)}");

                if (!File.Exists(SettingsPath))
                {                    
                    await SaveAsync(CreateDefaultSettings());
                    return CreateDefaultSettings();
                }

                var json = await File.ReadAllTextAsync(SettingsPath);
                System.Diagnostics.Debug.WriteLine($"RAW JSON: {json}"); // ← BU SATIR KRİTİK!

                var settings = JsonSerializer.Deserialize<UpdateSettingsModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // ← BU ÇOK ÖNEMLİ!
                });

                // Deserialize başarısız mı kontrol et
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ DESERIALIZE FAILED - returned null");
                    return CreateDefaultSettings();
                }

                System.Diagnostics.Debug.WriteLine($"✅ DESERIALIZE SUCCESS - AutoCheck: {settings.AutoCheckOnStartup}");
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 DESERIALIZE ERROR: {ex.Message}");
                return CreateDefaultSettings();
            }
        }

        public async Task SaveAsync(UpdateSettingsModel model)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

                var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Ekle
                });

                await File.WriteAllTextAsync(SettingsPath, json);

                System.Diagnostics.Debug.WriteLine($"Settings saved to: {SettingsPath}");
                System.Diagnostics.Debug.WriteLine($"Content: {json}"); // DEBUG
            }
            catch (Exception ex)
            {                
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
                throw;
            }
        }
        private UpdateSettingsModel CreateDefaultSettings()
        {
            return new UpdateSettingsModel
            {
                AutoCheckOnStartup = true,
                ShowNotifications = true,
                IncludeBetaVersions = false,
                LastCheckTime = null
            };
        }
    }
}

