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

        public void Configure(GitConfig config, bool dryRun)
        {
            if (!IsGitInstalled())
            {
                _logger.LogError("[Git] Error: Git is not installed/found in PATH.");
                return;
            }

            _logger.LogInfo("\n--- Configuring Git ---");

            if (!string.IsNullOrEmpty(config.UserName))
                SetGlobalConfig("user.name", config.UserName, dryRun);

            if (!string.IsNullOrEmpty(config.UserEmail))
                SetGlobalConfig("user.email", config.UserEmail, dryRun);

            if (!string.IsNullOrEmpty(config.SigningKey))
                SetGlobalConfig("user.signingkey", config.SigningKey, dryRun);

            if (config.CommitGpgSign.HasValue)
                SetGlobalConfig("commit.gpgsign", config.CommitGpgSign.Value.ToString().ToLower(), dryRun);

            if (config.Settings != null)
            {
                foreach (var setting in config.Settings)
                {
                    SetGlobalConfig(setting.Key, setting.Value, dryRun);
                }
            }
        }

        private void SetGlobalConfig(string key, string value, bool dryRun)
        {
            string currentValue = GetGlobalConfig(key);

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
            _processRunner.RunCommand("git", $"config --global {key} \"{value}\"", false);
        }

        private string GetGlobalConfig(string key)
        {
            string output = _processRunner.RunCommandWithOutput("git", $"config --global --get {key}");
            return output.Trim();
        }

        private bool IsGitInstalled()
        {
            return _processRunner.RunCommand("git", "--version", true);
        }
    }
}