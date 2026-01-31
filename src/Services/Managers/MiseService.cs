using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class MiseService : IPackageManager
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public MiseService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
        }

        private string GetMiseExecutable()
        {
            if (_processRunner.RunCommand("mise", "--version", false)) return "mise";

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string programData = Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData";

            string[] fallbacks = {
                Path.Combine(userProfile, "scoop", "shims", "mise.exe"),
                Path.Combine(programData, "scoop", "shims", "mise.exe")
            };

            foreach (var path in fallbacks)
            {
                if (File.Exists(path)) return path;
            }

            return "mise";
        }

        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        public void Install(AppConfig app, bool dryRun)
        {
            string executable = GetMiseExecutable();
            if (IsInstalled(app.Id))
            {
                _logger.LogInfo($"[Mise] {app.Id} is already set globally.");
                return;
            }

            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would set global '{app.Id}' via Mise");
                return;
            }

            _logger.LogInfo($"[Mise] Setting global {app.Id}...");
            string args = $"use --global {app.Id} -y";

            if (!_processRunner.RunCommand(executable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Mise.");
            }
            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            string executable = GetMiseExecutable();
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would remove global '{appId}' via Mise");
                return;
            }

            _logger.LogInfo($"[Mise] Removing global {appId}...");
            string args = $"unuse --global {appId}";

            if (!_processRunner.RunCommand(executable, args, false))
            {
                throw new Exception($"Failed to remove {appId} using Mise.");
            }
            _logger.LogSuccess($"[Success] Removed {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string executable = GetMiseExecutable();
            string output = _processRunner.RunCommandWithOutput(executable, "ls --global --current");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}