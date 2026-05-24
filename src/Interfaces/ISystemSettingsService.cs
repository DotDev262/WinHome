using WinHome.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinHome.Interfaces
{
    public interface ISystemSettingsService
    {
        Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object>? settings);
        Task ApplyNonRegistrySettingsAsync(Dictionary<string, object>? settings, bool dryRun);
        Task<Dictionary<string, object>> GetCapturedSettingsAsync();
        string? GetFriendlyName(string registryPath, string registryName);
        Task<Dictionary<string, object>> CaptureOriginalSettingsAsync(Dictionary<string, object> settings);
        Task RevertSystemSettingAsync(string settingKey, object originalValue, bool dryRun);
    }
}
