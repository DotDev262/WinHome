using YamlDotNet.Serialization;

namespace WinHome.Models.Plugins
{
    public class PluginManifest
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "1.0.0";

        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "executable"; // python, typescript, executable

        [YamlMember(Alias = "main")]
        public string Main { get; set; } = string.Empty;

        [YamlMember(Alias = "capabilities")]
        public List<string> Capabilities { get; set; } = new();

        // Internal path set during discovery
        public string DirectoryPath { get; set; } = string.Empty;
    }
}
