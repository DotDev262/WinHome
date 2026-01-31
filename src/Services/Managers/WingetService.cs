using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class WingetService : IPackageManager
    {
        private const string WingetExecutable = "winget";
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public WingetService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
        }

        public bool IsAvailable() => Bootstrapper.IsInstalled();

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                _logger.LogInfo($"[Winget] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would install '{app.Id}' via Winget");
                return;
            }

            _logger.LogInfo($"[Winget] Installing {app.Id}...");
            string args = $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements";
            if (!string.IsNullOrEmpty(app.Source)) args += $" --source {app.Source}";

            if (!_processRunner.RunCommand(WingetExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Winget.");
            }
            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Winget");
                return;
            }

            _logger.LogInfo($"[Winget] Uninstalling {appId}...");
            string args = $"uninstall --id {appId} -e --silent --accept-source-agreements";

            if (!_processRunner.RunCommand(WingetExecutable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Winget.");
            }
            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            var output = _processRunner.RunCommandWithOutput(WingetExecutable, $"list -q {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}