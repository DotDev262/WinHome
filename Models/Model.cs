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

        [YamlMember(Alias = "git")]
        public GitConfig? Git { get; set; }

        [YamlMember(Alias = "profiles")]
        public Dictionary<string, ProfileConfig> Profiles { get; set; } = new();

        [YamlMember(Alias = "envVars")]
        public List<EnvVarConfig> EnvVars { get; set; } = new();
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

     public class GitConfig
    {
        // Convenience properties (Common stuff)
        [YamlMember(Alias = "userName")]
        public string? UserName { get; set; }

        [YamlMember(Alias = "userEmail")]
        public string? UserEmail { get; set; }

        [YamlMember(Alias = "signingKey")]
        public string? SigningKey { get; set; }

        [YamlMember(Alias = "commitGpgSign")]
        public bool? CommitGpgSign { get; set; }

        // NEW: Generic Dictionary for EVERYTHING else
        // Maps "core.editor" -> "code --wait"
        [YamlMember(Alias = "settings")]
        public Dictionary<string, string> Settings { get; set; } = new();
    }

     public class ProfileConfig
    {
        [YamlMember(Alias = "git")]
        public GitConfig? Git { get; set; }
        
        // You can add 'Apps' or 'SystemSettings' here later if you want profile-specific apps
    }

    public class EnvVarConfig
    {
        [YamlMember(Alias = "variable")]
        public string Variable { get; set; } = string.Empty;

        [YamlMember(Alias = "value")]
        public string Value { get; set; } = string.Empty;

        [YamlMember(Alias = "action")]
        public string Action { get; set; } = "set"; 
    }

}