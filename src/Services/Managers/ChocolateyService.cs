using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ChocolateyService : IPackageManager
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public ChocolateyService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
        }

        private string GetChocoExecutable()
        {
            if (_processRunner.RunCommand("choco", "--version", false)) return "choco";

            string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe");
            if (File.Exists(chocoPath)) return chocoPath;

            return "choco";
        }

        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        public void Install(AppConfig app, bool dryRun)
        {
            string executable = GetChocoExecutable();
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

            if (!_processRunner.RunCommand(executable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Chocolatey.");
            }

            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            string executable = GetChocoExecutable();
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Chocolatey");
                return;
            }

            _logger.LogInfo($"[Choco] Uninstalling {appId}...");
            string args = $"uninstall {appId} -y";

            if (!_processRunner.RunCommand(executable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Chocolatey.");
            }

            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string executable = GetChocoExecutable();
            string output = _processRunner.RunCommandWithOutput(executable, $"list -l -r {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}