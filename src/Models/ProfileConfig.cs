using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
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
