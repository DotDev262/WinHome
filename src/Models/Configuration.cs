using System.Collections.Generic;
using System.Text.Json.Serialization;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class Configuration
    {
        [YamlMember(Alias = "version")]
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [YamlMember(Alias = "apps")]
        [JsonPropertyName("apps")]
        public List<AppConfig> Apps { get; set; } = new();

        [YamlMember(Alias = "registryTweaks")]
        [JsonPropertyName("registryTweaks")]
        public List<RegistryTweak> RegistryTweaks { get; set; } = new();

        [YamlMember(Alias = "dotfiles")]
        [JsonPropertyName("dotfiles")]
        public List<DotfileConfig> Dotfiles { get; set; } = new();

        [YamlMember(Alias = "systemSettings")]
        [JsonPropertyName("systemSettings")]
        public Dictionary<string, object> SystemSettings { get; set; } = new();

        [YamlMember(Alias = "wsl")]
        [JsonPropertyName("wsl")]
        public WslConfig? Wsl { get; set; }

        [YamlMember(Alias = "git")]
        [JsonPropertyName("git")]
        public GitConfig? Git { get; set; }

        [YamlMember(Alias = "profiles")]
        [JsonPropertyName("profiles")]
        public Dictionary<string, ProfileConfig> Profiles { get; set; } = new();

        [YamlMember(Alias = "envVars")]
        [JsonPropertyName("envVars")]
        public List<EnvVarConfig> EnvVars { get; set; } = new();

        [YamlMember(Alias = "services")]
        [JsonPropertyName("services")]
        public List<WindowsServiceConfig> Services { get; set; } = new();

        [YamlMember(Alias = "scheduledTasks")]
        [JsonPropertyName("scheduledTasks")]
        public List<ScheduledTaskConfig> ScheduledTasks { get; set; } = new();

        [YamlMember(Alias = "extensions")]
        [JsonPropertyName("extensions")]
        public Dictionary<string, object> Extensions { get; set; } = new();
    }
}
