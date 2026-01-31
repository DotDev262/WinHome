using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class GitService : IGitService
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;

        public GitService(IProcessRunner processRunner, ILogger logger)
        {
            _processRunner = processRunner;
            _logger = logger;
        }

        private string GetGitExecutable()
        {
            if (_processRunner.RunCommand("git", "--version", false)) return "git";

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string programData = Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData";
            
            // Fallback for fresh Scoop installs (various possible locations)
            string[] fallbacks = {
                Path.Combine(userProfile, "scoop", "shims", "git.exe"),
                Path.Combine(programData, "scoop", "shims", "git.exe"),
                Path.Combine(userProfile, "scoop", "apps", "git", "current", "cmd", "git.exe"),
                Path.Combine(programData, "scoop", "apps", "git", "current", "cmd", "git.exe")
            };

            foreach (var path in fallbacks)
            {
                if (File.Exists(path)) return path;
            }

            return "git";
        }

        public void Configure(GitConfig config, bool dryRun)
        {
            if (!IsInstalled())
            {
                _logger.LogError("[Git] Error: Git is not installed/found in PATH.");
                return;
            }

            _logger.LogInfo("\n--- Configuring Git ---");
            string gitExec = GetGitExecutable();

            if (!string.IsNullOrEmpty(config.UserName))
                SetGlobalConfig(gitExec, "user.name", config.UserName, dryRun);

            if (!string.IsNullOrEmpty(config.UserEmail))
                SetGlobalConfig(gitExec, "user.email", config.UserEmail, dryRun);

            if (!string.IsNullOrEmpty(config.SigningKey))
                SetGlobalConfig(gitExec, "user.signingkey", config.SigningKey, dryRun);

            if (config.CommitGpgSign.HasValue)
                SetGlobalConfig(gitExec, "commit.gpgsign", config.CommitGpgSign.Value.ToString().ToLower(), dryRun);

            if (config.Settings != null)
            {
                foreach (var setting in config.Settings)
                {
                    SetGlobalConfig(gitExec, setting.Key, setting.Value, dryRun);
                }
            }
        }

        private void SetGlobalConfig(string gitExec, string key, string value, bool dryRun)
        {
            string currentValue = GetGlobalConfig(gitExec, key);

            if (string.Equals(currentValue, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would set git config --global {key} \"{value}\"");
                return;
            }

            _logger.LogInfo($"[Git] Setting {key} = {value}...");
            _processRunner.RunCommand(gitExec, $"config --global {key} \"{value}\"", false);
        }

        private string GetGlobalConfig(string gitExec, string key)
        {
            string output = _processRunner.RunCommandWithOutput(gitExec, $"config --global --get {key}");
            return output.Trim();
        }

        public bool IsInstalled()
        {
            if (_processRunner.RunCommand("git", "--version", false)) return true;

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string programData = Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData";

            return File.Exists(Path.Combine(userProfile, "scoop", "shims", "git.exe")) ||
                   File.Exists(Path.Combine(programData, "scoop", "shims", "git.exe")) ||
                   File.Exists(Path.Combine(userProfile, "scoop", "apps", "git", "current", "cmd", "git.exe")) ||
                   File.Exists(Path.Combine(programData, "scoop", "apps", "git", "current", "cmd", "git.exe"));
        }
    }
}