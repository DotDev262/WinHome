using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
  /// <summary>Represents a named profile containing environment variable and Git configuration overrides.</summary>
  public class ProfileConfig
  {
    [YamlMember(Alias = "git")]
    [JsonPropertyName("git")]
    public GitConfig? Git { get; set; }

    [YamlMember(Alias = "envVars")]
    [JsonPropertyName("envVars")]
    public List<EnvVarConfig> EnvVars { get; set; } = new();
  }
}
