using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
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
                if (dryRun)
                {
                    _logger.LogWarning("[DryRun] WSL is not detected/active. Simulating subsequent actions...");
                }
                else
                {
                    _logger.LogError("[WSL] Error: WSL is not active. Run 'wsl --install' in Admin Terminal and reboot.");
                    return;
                }
            }

            if (config.Update)
            {
                if (dryRun) _logger.LogWarning("[DryRun] Would run 'wsl --update'");
                else _processRunner.RunCommand("wsl", "--update", false);
            }

            if (config.DefaultVersion > 0)
            {
                if (dryRun) _logger.LogWarning($"[DryRun] Would set WSL default version to {config.DefaultVersion}");
                else _processRunner.RunCommand("wsl", $"--set-default-version {config.DefaultVersion}", false);
            }

            if (config.Distros.Any())
            {
                _logger.LogInfo("\n--- Configuring WSL Distros ---");
                foreach (var distro in config.Distros)
                {
                    bool installed = EnsureDistro(distro, dryRun);

                    if (!string.IsNullOrEmpty(config.DefaultDistro) && config.DefaultDistro == distro.Name)
                    {
                        if (dryRun) _logger.LogWarning($"[DryRun] Would set default distro to '{distro.Name}'");
                        else _processRunner.RunCommand("wsl", $"--set-default {distro.Name}", false);
                    }

                    if (installed || dryRun)
                    {
                        ProvisionDistro(distro, dryRun);
                    }
                }
            }
        }

        private bool EnsureDistro(WslDistroConfig distro, bool dryRun)
        {
            if (IsDistroInstalled(distro.Name))
            {
                return true;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would install WSL Distro: {distro.Name}");
                return false;
            }

            _logger.LogInfo($"[WSL] Installing {distro.Name}...");
            _logger.LogInfo("[WSL] NOTE: A new window will open for you to create your UNIX username/password.");

            if (_processRunner.RunCommand("wsl", $"--install -d {distro.Name}", false))
            {
                _logger.LogSuccess($"[Success] {distro.Name} installed.");
                return true;
            }
            else
            {
                _logger.LogError($"[Error] Failed to install {distro.Name}");
                return false;
            }
        }

        private void ProvisionDistro(WslDistroConfig distro, bool dryRun)
        {
            if (string.IsNullOrEmpty(distro.SetupScript)) return;

            string localScriptPath = Path.GetFullPath(distro.SetupScript);
            if (!File.Exists(localScriptPath))
            {
                _logger.LogError($"[WSL] Error: Script not found {localScriptPath}");
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would execute '{Path.GetFileName(localScriptPath)}' inside {distro.Name}");
                return;
            }

            _logger.LogInfo($"[WSL] Provisioning {distro.Name} with {Path.GetFileName(localScriptPath)}...");

            try
            {
                string scriptContent = File.ReadAllText(localScriptPath).Replace("\r\n", "\n");
                var output = _processRunner.RunCommandWithOutput("wsl", $"-d {distro.Name} -- bash -s", scriptContent);

                if (!string.IsNullOrEmpty(output))
                {
                    _logger.LogInfo(output.Trim());
                    _logger.LogSuccess($"[Success] {distro.Name} configured.");
                }
                else
                {
                    _logger.LogError($"[Error] Script failed in {distro.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Failed to execute script: {ex.Message}");
            }
        }

        private bool IsDistroInstalled(string distroName)
        {
            string output = _processRunner.RunCommandWithOutput("wsl", "--list --verbose");
            return output.Contains(distroName, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWslInstalled()
        {
            return _processRunner.RunCommand("wsl", "--status", true);
        }
    }
}