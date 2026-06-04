using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
  /// <summary>Configuration for a single WSL distribution (distro).</summary>
  public class WslDistroConfig
  {
    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "setupScript")]
    [JsonPropertyName("setupScript")]
    public string? SetupScript { get; set; }
  }
}
