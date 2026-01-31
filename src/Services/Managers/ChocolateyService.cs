using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ChocolateyService : IPackageManager
    {
        private const string ChocoExecutable = "choco";
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public ChocolateyService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
        }

        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                _logger.LogInfo($"[Choco] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would install '{app.Id}' via Chocolatey");
                return;
            }

            _logger.LogInfo($"[Choco] Installing {app.Id}...");

            string args = $"install {app.Id} -y";

            if (!_processRunner.RunCommand(ChocoExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Chocolatey.");
            }

            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Chocolatey");
                return;
            }

            _logger.LogInfo($"[Choco] Uninstalling {appId}...");
            string args = $"uninstall {appId} -y";

            if (!_processRunner.RunCommand(ChocoExecutable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Chocolatey.");
            }

            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {

            string output = _processRunner.RunCommandWithOutput(ChocoExecutable, $"list -l -r {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}