using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
  /// <summary>
  /// Represents the complete system state tracked by WinHome, including applied items
  /// and original values of system settings for reverting when they're removed from config.
  /// </summary>
  public class StateData
  {
    [JsonPropertyName("applied_items")]
    public HashSet<string> AppliedItems { get; set; } = new();

    [JsonPropertyName("system_setting_originals")]
    public Dictionary<string, object> SystemSettingOriginals { get; set; } = new();
  }
}
