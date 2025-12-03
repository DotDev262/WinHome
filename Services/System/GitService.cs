using System.Diagnostics;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class GitService
    {
        public void Configure(GitConfig config, bool dryRun)
        {
            if (!IsGitInstalled())
            {
                Console.WriteLine("[Git] Error: Git is not installed/found in PATH.");
                return;
            }

            Console.WriteLine("\n--- Configuring Git ---");

            // 1. Handle Explicit Properties (Convenience)
            if (!string.IsNullOrEmpty(config.UserName))
                SetGlobalConfig("user.name", config.UserName, dryRun);

            if (!string.IsNullOrEmpty(config.UserEmail))
                SetGlobalConfig("user.email", config.UserEmail, dryRun);

            if (!string.IsNullOrEmpty(config.SigningKey))
                SetGlobalConfig("user.signingkey", config.SigningKey, dryRun);

            if (config.CommitGpgSign.HasValue)
                SetGlobalConfig("commit.gpgsign", config.CommitGpgSign.Value.ToString().ToLower(), dryRun);

            // 2. Handle Generic Settings Dictionary
            // This supports ANY git config key (e.g. core.editor, pull.rebase, init.defaultBranch)
            if (config.Settings != null)
            {
                foreach (var setting in config.Settings)
                {
                    // setting.Key is "init.defaultBranch"
                    // setting.Value is "main"
                    SetGlobalConfig(setting.Key, setting.Value, dryRun);
                }
            }
        }

        private void SetGlobalConfig(string key, string value, bool dryRun)
        {
            // Idempotency: Check if it's already set
            string currentValue = GetGlobalConfig(key);

            if (string.Equals(currentValue, value, StringComparison.OrdinalIgnoreCase))
            {
                // Already set, stay silent to keep output clean
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would set git config --global {key} \"{value}\"");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Git] Setting {key} = {value}...");
            RunGit($"config --global {key} \"{value}\"");
        }

        private string GetGlobalConfig(string key)
        {
            string output = RunGitWithOutput($"config --global --get {key}");
            return output.Trim();
        }

        private bool IsGitInstalled()
        {
            return RunGit("--version", silent: true);
        }

        private bool RunGit(string args, bool silent = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = silent
            };
            try 
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch { return false; }
        }

        private string RunGitWithOutput(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
                process?.WaitForExit();
                return output;
            }
            catch { return string.Empty; }
        }
    }
}