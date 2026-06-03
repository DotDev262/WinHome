using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
  /// <summary>Represents a single Windows Registry value to create, modify, or delete.</summary>
  public class RegistryTweak
  {
    [YamlMember(Alias = "path")]
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "value")]
    [JsonPropertyName("value")]
    public object Value { get; set; } = new();

    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
  }
}
