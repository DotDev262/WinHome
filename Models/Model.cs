using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class Configuration
    {
        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "1.0";

        [YamlMember(Alias = "apps")]

        public List<AppConfig> Apps { get; set; } = new();
        [YamlMember(Alias = "registryTweaks")]
        public List<RegistryTweak> RegistryTweaks { get; set; } = new();

        [YamlMember(Alias = "dotfiles")]
        public List<DotfileConfig> Dotfiles { get; set; } = new();

        [YamlMember(Alias = "systemSettings")]
        public Dictionary<string, object> SystemSettings { get; set; } = new();

        [YamlMember(Alias = "wsl")]
        public WslConfig? Wsl { get; set; }
    }

    public class AppConfig
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Alias = "source")]
        public string? Source { get; set; }

        [YamlMember(Alias = "manager")]
        public string Manager { get; set; } = "winget";
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
        public string Type { get; set; } = "string"; 
    }

    public class DotfileConfig
    {
        [YamlMember(Alias = "src")]
        public string Src { get; set; } = string.Empty;

        [YamlMember(Alias = "target")]
        public string Target { get; set; } = string.Empty;
    }

    public class WslConfig
    {
        [YamlMember(Alias = "defaultVersion")]
        public int DefaultVersion { get; set; } = 2;

        [YamlMember(Alias = "defaultDistro")]
        public string? DefaultDistro { get; set; }

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;

        
        [YamlMember(Alias = "distros")]
        public List<WslDistroConfig> Distros { get; set; } = new();
    }

    public class WslDistroConfig
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty; 

        [YamlMember(Alias = "setupScript")]
        public string? SetupScript { get; set; }
    }
}