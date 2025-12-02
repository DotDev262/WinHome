using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class Configuration
    {
        [YamlMember(Alias ="version")]
        public string Version{get;set;} = "1.0";

        [YamlMember(Alias ="apps")]

        public List<AppConfig> Apps {get;set;} = new();
        [YamlMember(Alias ="registry_tweaks")]
        public List<RegistryTweak> RegistryTweaks {get;set;} = new();

        [YamlMember(Alias ="dotfiles")]
        public List<DotfileConfig> Dotfiles {get;set;} = new();
    }

    public class AppConfig
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Alias = "source")]
        public string? Source { get; set; }
    }

    public class RegistryTweak
    {
        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "value")]
        public object Value { get; set; } = new();

        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "string"; // We default to "string"
    }

    public class DotfileConfig
    {
        [YamlMember(Alias = "src")]
        public string Src { get; set; } = string.Empty;

        [YamlMember(Alias = "target")]
        public string Target { get; set; } = string.Empty;
    }
}