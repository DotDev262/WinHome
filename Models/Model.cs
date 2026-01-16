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

    public class ScheduledTaskConfig
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        [YamlMember(Alias = "author")]
        public string? Author { get; set; }

        [YamlMember(Alias = "triggers")]
        public List<TriggerConfig> Triggers { get; set; } = new();

        [YamlMember(Alias = "actions")]
        public List<ActionConfig> Actions { get; set; } = new();
    }

    public class TriggerConfig
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = string.Empty;

        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; } = true;

        [YamlMember(Alias = "startBoundary")]
        public DateTime? StartBoundary { get; set; }

        [YamlMember(Alias = "endBoundary")]
        public DateTime? EndBoundary { get; set; }

        [YamlMember(Alias = "executionTimeLimit")]
        public TimeSpan? ExecutionTimeLimit { get; set; }

        [YamlMember(Alias = "id")]
        public string? Id { get; set; }

        [YamlMember(Alias = "repetition")]
        public RepetitionPatternConfig? Repetition { get; set; }

        [YamlMember(Alias = "delay")]
        public TimeSpan? Delay { get; set; }
    }

    public class RepetitionPatternConfig
    {
        [YamlMember(Alias = "interval")]
        public TimeSpan Interval { get; set; }

        [YamlMember(Alias = "duration")]
        public TimeSpan Duration { get; set; }

        [YamlMember(Alias = "stopAtDurationEnd")]
        public bool StopAtDurationEnd { get; set; }
    }

    public class ActionConfig
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "arguments")]
        public string? Arguments { get; set; }

        [YamlMember(Alias = "workingDirectory")]
        public string? WorkingDirectory { get; set; }
    }


    public class WindowsServiceConfig
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "state")]
        public string State { get; set; } = "running"; // running, stopped

        [YamlMember(Alias = "startup")]
        public string? StartupType { get; set; } // automatic, manual, disabled
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