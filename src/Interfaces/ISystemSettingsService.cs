using WinHome.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinHome.Interfaces
{
  /// <summary>Service for managing Windows system settings (both registry and non-registry).</summary>
  public interface ISystemSettingsService
  {
    /// <summary>Converts system setting key-value pairs into registry tweaks.</summary>
    Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object>? settings);
    /// <summary>Applies non-registry system settings.</summary>
    Task ApplyNonRegistrySettingsAsync(Dictionary<string, object>? settings, bool dryRun);
    /// <summary>Captures current system settings into a dictionary.</summary>
    Task<Dictionary<string, object>> GetCapturedSettingsAsync();
    /// <summary>Gets a human-readable friendly name for a registry setting path.</summary>
    string? GetFriendlyName(string registryPath, string registryName);
    /// <summary>Captures the original values of system settings before modification.</summary>
    Task<Dictionary<string, object>> CaptureOriginalSettingsAsync(Dictionary<string, object> settings);
    /// <summary>Reverts a system setting to its original value.</summary>
    Task RevertSystemSettingAsync(string settingKey, object originalValue, bool dryRun);
  }
}
