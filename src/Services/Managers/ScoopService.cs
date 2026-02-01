using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ScoopService : IPackageManager
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public ScoopService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
        }

        private string GetScoopExecutable()
        {
            if (_processRunner.RunCommand("scoop", "--version", false)) return "scoop.cmd";
            
            string[] paths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData", "scoop", "shims", "scoop.cmd")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path)) return path;
            }

            return "scoop.cmd"; // Fallback to original behavior
        }

        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        public void Install(AppConfig app, bool dryRun)
        {
            string executable = GetScoopExecutable();
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

            if (!_processRunner.RunCommand(executable, args, false, line => _logger.LogInfo($"[Scoop:Install] {line}")))
            {
                throw new Exception($"Failed to install {app.Id} using Scoop.");
            }
            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            string executable = GetScoopExecutable();
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Scoop");
                return;
            }

            _logger.LogInfo($"[Scoop] Uninstalling {appId}...");
            string args = $"uninstall {appId}";

            if (!_processRunner.RunCommand(executable, args, false, line => _logger.LogInfo($"[Scoop:Uninstall] {line}")))
            {
                throw new Exception($"Failed to uninstall {appId} using Scoop.");
            }
            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string executable = GetScoopExecutable();
            string output = _processRunner.RunCommandWithOutput(executable, "list");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}