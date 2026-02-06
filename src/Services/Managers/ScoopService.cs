using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;

namespace WinHome.Services.Managers
{
    public class ScoopService : IPackageManager
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        private readonly IRuntimeResolver _resolver;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public ScoopService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger, IRuntimeResolver resolver)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
            _resolver = resolver;
        }

        private string GetScoopExecutable()
        {
            return _resolver.Resolve("scoop");
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

            bool alreadyInstalled = false;
            bool manifestNotFound = false;
            bool success = _processRunner.RunCommand(executable, args, false, line => 
            {
                if (line == null) return;
                _logger.LogInfo($"[Scoop:Install] {line}");
                if (line.Contains($"'{app.Id}' is already installed", StringComparison.OrdinalIgnoreCase))
                {
                    alreadyInstalled = true;
                }
                if (line.Contains("Couldn't find manifest", StringComparison.OrdinalIgnoreCase))
                {
                    manifestNotFound = true;
                }
            });

            if (!success || manifestNotFound)
            {
                if (alreadyInstalled)
                {
                    _logger.LogSuccess($"[Success] {app.Id} is already installed (detected during install attempt).");
                    return;
                }
                throw new Exception($"Failed to install {app.Id} using Scoop.{(manifestNotFound ? " Manifest not found." : "")}");
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