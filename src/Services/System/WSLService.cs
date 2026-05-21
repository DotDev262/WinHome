using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Manages WSL distribution lifecycles, configuration, and automated provisioning.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WslService : IWslService
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;

        public WslService(IProcessRunner processRunner, ILogger logger)
        {
            _processRunner = processRunner;
            _logger = logger;
        }

        public void Configure(WslConfig config, bool dryRun)
        {
            if (!IsWslInstalled())
            {
                if (dryRun) _logger.LogWarning("[DryRun] WSL is not detected. Simulating configuration...");
                else
                {
                    _logger.LogError("[WSL] Error: WSL is not active. Please run 'wsl --install' in an Administrative Terminal and reboot.");
                    return;
                }
            }

            if (config.Update)
            {
                if (dryRun) _logger.LogWarning("[DryRun] Would execute: wsl --update");
                else _processRunner.RunCommand("wsl", "--update", false);
            }

            if (config.DefaultVersion > 0)
            {
                if (dryRun) _logger.LogWarning($"[DryRun] Would set WSL default version to {config.DefaultVersion}");
                else _processRunner.RunCommand("wsl", $"--set-default-version {config.DefaultVersion}", false);
            }

            foreach (var distro in config.Distros)
            {
                _logger.LogInfo($"--- Configuring Distribution: {distro.Name} ---");
                bool installed = EnsureDistro(distro, dryRun);

                if (!string.IsNullOrEmpty(config.DefaultDistro) && config.DefaultDistro.Equals(distro.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (dryRun) _logger.LogWarning($"[DryRun] Would set '{distro.Name}' as default distro.");
                    else _processRunner.RunCommand("wsl", $"--set-default {distro.Name}", false);
                }

                if (installed || dryRun)
                {
                    ProvisionDistro(distro, dryRun);
                }
            }
        }

        private bool EnsureDistro(WslDistroConfig distro, bool dryRun)
        {
            if (IsDistroInstalled(distro.Name)) return true;

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would install WSL Distro: {distro.Name}");
                return false;
            }

            _logger.LogInfo($"[WSL] Installing {distro.Name}... This may take a few minutes.");
            
            bool success = _processRunner.RunCommand("wsl", $"--install -d {distro.Name}", false);
            if (success) _logger.LogSuccess($"[Success] {distro.Name} installed successfully.");
            else _logger.LogError($"[Error] Failed to install {distro.Name}. Check your internet connection.");
            
            return success;
        }

        private void ProvisionDistro(WslDistroConfig distro, bool dryRun)
        {
            if (string.IsNullOrEmpty(distro.SetupScript)) return;

            string scriptPath = Path.GetFullPath(distro.SetupScript);
            if (!File.Exists(scriptPath))
            {
                _logger.LogError($"[WSL] Provisioning error: Script not found at {scriptPath}");
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would execute setup script '{Path.GetFileName(scriptPath)}' in {distro.Name}");
                return;
            }

            _logger.LogInfo($"[WSL] Provisioning {distro.Name}...");

            try
            {
                // Normalize line endings for Linux bash environment
                string scriptContent = File.ReadAllText(scriptPath).Replace("\r\n", "\n");
                string output = _processRunner.RunCommandWithOutput("wsl", $"-d {distro.Name} -- bash -s", scriptContent);

                if (!string.IsNullOrEmpty(output)) _logger.LogInfo(output.Trim());
                _logger.LogSuccess($"[Success] {distro.Name} provisioning complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[WSL] Provisioning failed: {ex.Message}");
            }
        }

        private bool IsDistroInstalled(string distroName)
        {
            string output = _processRunner.RunCommandWithOutput("wsl", "--list --verbose");
            return output.Contains(distroName, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWslInstalled()
        {
            // Simple exit code check to verify WSL availability
            return _processRunner.RunCommand("wsl", "--status", false);
        }
    }
}