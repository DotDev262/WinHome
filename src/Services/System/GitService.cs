using System.IO;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides global configuration deployment handlers for Git version control client environments, 
    /// resolving system binary paths across standard environment locations and localized Scoop setups.
    /// </summary>
    public class GitService : IGitService
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitService"/> class with specialized shell runtimes and telemetry.
        /// </summary>
        /// <param name="processRunner">The low-level OS shell execution utility handling binary streams.</param>
        /// <param name="logger">The diagnostic log tracker capture utility routing system statuses or errors.</param>
        public GitService(IProcessRunner processRunner, ILogger logger)
        {
            _processRunner = processRunner;
            _logger = logger;
        }

        /// <summary>
        /// Probes system storage patterns to extract the explicit file execution path of the local Git binary client, 
        /// matching system pathways and dynamic shims before falling back to shell alias defaults.
        /// </summary>
        /// <returns>The absolute filesystem file path to the direct executable binary link if located; otherwise, <c>"git"</c>.</returns>
        private string GetGitExecutable()
        {
            if (_processRunner.RunCommand("git", "--version", false)) return "git";

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string programData = Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData";

            // Fallback tracking matrices for fresh Scoop tool installations across distinct user/machine structures
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

        /// <summary>
        /// Applies identity keys, metadata parameters, encryption signing requirements, and custom sub-property vectors 
        /// into the global runtime configuration schema.
        /// </summary>
        /// <param name="config">The parsed workspace data settings map containing desired user profile rules.</param>
        /// <param name="dryRun">A conditional flag which, when <c>true</c>, skips system mutation execution loops entirely.</param>
        public void Configure(GitConfig config, bool dryRun)
        {
            if (!IsInstalled())
            {
                _logger.LogError("[Git] Error: Git is not installed/found in PATH.");
                return;
            }

            _logger.LogSuccess("[Git] Initializing Git configuration pipeline step...");
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

        /// <summary>
        /// Modifies a targeted global configurations property variable state atomically after conducting target idempotency evaluations.
        /// </summary>
        /// <param name="gitExec">The absolute location string referencing the tracked git executable process on disk.</param>
        /// <param name="key">The explicit config flag namespace sequence identifier to configure (e.g., <c>"user.name"</c>).</param>
        /// <param name="value">The payload text assignment configuration option mapping property parameters.</param>
        /// <param name="dryRun">A conditional execution safety modifier flag which, when <c>true</c>, outputs intended operations to logs without applying mutations.</param>
        private void SetGlobalConfig(string gitExec, string key, string value, bool dryRun)
        {
            string currentValue = GetGlobalConfig(gitExec, key);

            if (string.Equals(currentValue, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (dryRun)
            {
                _logger.LogError($"[DryRun] Would set git config --global {key} \"{value}\"");
                return;
            }

            _logger.LogSuccess($"[Git] Setting {key} = {value}...");
            _processRunner.RunCommand(gitExec, $"config --global {key} \"{value}\"", false);
        }

        /// <summary>
        /// Inspects the underlying Git client engine registry context flags directly to isolate structural configuration settings.
        /// </summary>
        /// <param name="gitExec">The absolute location string referencing the tracked git executable process on disk.</param>
        /// <param name="key">The specific operational target configuration key string to look up.</param>
        /// <returns>The string context value currently assigned to the flag key inside global settings files.</returns>
        private string GetGlobalConfig(string gitExec, string key)
        {
            string output = _processRunner.RunCommandWithOutput(gitExec, $"config --global --get {key}");
            return output.Trim();
        }

        /// <summary>
        /// Validates if a usable instantiation platform framework setup for the Git engine binary is 
        /// discoverable via runtime environment arrays or sandbox structures.
        /// </summary>
        /// <returns><c>true</c> if a functional executable pointer route evaluates cleanly; otherwise, <c>false</c>.</returns>
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