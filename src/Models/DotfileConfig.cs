using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
  /// <summary>Represents a dotfile or directory to symlink or copy from source to target.</summary>
  public class DotfileConfig : ResourceBase
  {
    [YamlMember(Alias = "src")]
    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;

    [YamlMember(Alias = "target")]
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
  }
}
