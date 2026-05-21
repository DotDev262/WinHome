using System.Collections.Generic;
using System.Text.Json.Serialization;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Root configuration object containing all system initialization settings and modules.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Gets or sets the schema version of the configuration file. Defaults to "1.0".
        /// </summary>
        [YamlMember(Alias = "version")]
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the list of application deployment configurations.
        /// </summary>
        [YamlMember(Alias = "apps")]
        [JsonPropertyName("apps")]
        public List<AppConfig> Apps { get; set; } = new();

        /// <summary>
        /// Gets or sets the registry entries and configurations to modify.
        /// </summary>
        [YamlMember(Alias = "registryTweaks")]
        [JsonPropertyName("registryTweaks")]
        public List<RegistryTweak> RegistryTweaks { get; set; } = new();

        /// <summary>
        /// Gets or sets user dotfiles synchronization mapping patterns.
        /// </summary>
        [YamlMember(Alias = "dotfiles")]
        [JsonPropertyName("dotfiles")]
        public List<DotfileConfig> Dotfiles { get; set; } = new();

        /// <summary>
        /// Gets or sets key-value pairs representing unstructured core system configurations.
        /// </summary>
        [YamlMember(Alias = "systemSettings")]
        [JsonPropertyName("systemSettings")]
        public Dictionary<string, object> SystemSettings { get; set; } = new();

        /// <summary>
        /// Gets or sets the Windows Subsystem for Linux configuration segment.
        /// </summary>
        [YamlMember(Alias = "wsl")]
        [JsonPropertyName("wsl")]
        public WslConfig? Wsl { get; set; }

        /// <summary>
        /// Gets or sets the global Git version control parameters.
        /// </summary>
        [YamlMember(Alias = "git")]
        [JsonPropertyName("git")]
        public GitConfig? Git { get; set; }

        /// <summary>
        /// Gets or sets specific profiles that override configuration behaviors based on execution context.
        /// </summary>
        [YamlMember(Alias = "profiles")]
        [JsonPropertyName("profiles")]
        public Dictionary<string, ProfileConfig> Profiles { get; set; } = new();

        /// <summary>
        /// Gets or sets the user environment variables to establish or overwrite.
        /// </summary>
        [YamlMember(Alias = "envVars")]
        [JsonPropertyName("envVars")]
        public List<EnvVarConfig> EnvVars { get; set; } = new();

        /// <summary>
        /// Gets or sets Windows system services to monitor or maintain states for.
        /// </summary>
        [YamlMember(Alias = "services")]
        [JsonPropertyName("services")]
        public List<WindowsServiceConfig> Services { get; set; } = new();

        /// <summary>
        /// Gets or sets cron or system startup tasks to schedule on the host device.
        /// </summary>
        [YamlMember(Alias = "scheduledTasks")]
        [JsonPropertyName("scheduledTasks")]
        public List<ScheduledTaskConfig> ScheduledTasks { get; set; } = new();

        /// <summary>
        /// Gets or sets extension plugin specific parameters and configuration values.
        /// </summary>
        [YamlMember(Alias = "extensions")]
        [JsonPropertyName("extensions")]
        public Dictionary<string, object> Extensions { get; set; } = new();

        /// <summary>
        /// Gets or sets configuration context fields explicit to the Vim editor.
        /// </summary>
        [YamlMember(Alias = "vim")]
        [JsonPropertyName("vim")]
        public object? Vim { get; set; }

        /// <summary>
        /// Gets or sets configuration context fields explicit to VS Code.
        /// </summary>
        [YamlMember(Alias = "vscode")]
        [JsonPropertyName("vscode")]
        public object? Vscode { get; set; }

        /// <summary>
        /// Gets or sets configuration context fields explicit to the Obsidian environment.
        /// </summary>
        [YamlMember(Alias = "obsidian")]
        [JsonPropertyName("obsidian")]
        public object? Obsidian { get; set; }

        /// <summary>
        /// Gets or sets configuration context fields explicit to Oh My Posh shell prompt engines.
        /// </summary>
        [YamlMember(Alias = "ohmyposh")]
        [JsonPropertyName("ohmyposh")]
        public object? Ohmyposh { get; set; }
    }
}