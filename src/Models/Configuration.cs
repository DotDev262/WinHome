using System.Collections.Generic;
using WinHome.Models;
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

        [YamlMember(Alias = "services")]
        public List<WindowsServiceConfig> Services { get; set; } = new();

        [YamlMember(Alias = "scheduledTasks")]
        public List<ScheduledTaskConfig> ScheduledTasks { get; set; } = new();
    }
}
