using YamlDotNet.Serialization;

namespace WinHome.Models.Plugins
{
  /// <summary>Describes a plugin's metadata from its manifest file (plugin.yaml).</summary>
  public class PluginManifest
  {
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "1.0.0";

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "executable";

    [YamlMember(Alias = "main")]
    public string Main { get; set; } = string.Empty;

    [YamlMember(Alias = "capabilities")]
    public List<string> Capabilities { get; set; } = new();

    public string DirectoryPath { get; set; } = string.Empty;
  }
}
