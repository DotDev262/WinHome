using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ScoopService : IPackageManager
    {
        private const string ScoopExecutable = "scoop.cmd";
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public ScoopService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
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
                _logger.LogInfo($"[Scoop] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would install '{app.Id}' via Scoop");
                return;
            }

            _logger.LogInfo($"[Scoop] Installing {app.Id}...");
            string args = $"install {app.Id}";

            if (!_processRunner.RunCommand(ScoopExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Scoop.");
            }
            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Scoop");
                return;
            }

            _logger.LogInfo($"[Scoop] Uninstalling {appId}...");
            string args = $"uninstall {appId}";

            if (!_processRunner.RunCommand(ScoopExecutable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Scoop.");
            }
            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string output = _processRunner.RunCommandWithOutput(ScoopExecutable, "list");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}