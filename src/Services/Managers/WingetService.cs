using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;

namespace WinHome.Services.Managers
{
    public class WingetService : IPackageManager
    {
        private string _wingetPath = "winget";
        private bool _pathResolved = false;
        private bool _sourceUpdated = false;
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public WingetService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
        }

        private void ResolveWingetPath()
        {
            if (_pathResolved && _wingetPath != "winget") return;

            _logger.LogInfo("[Winget] Verifying winget installation...");
            // Try quick check first
            if (_processRunner.RunCommand("winget", "--version", false, line => LogFiltered(line, "Output")))
            {
                _wingetPath = "winget";
                _pathResolved = true;
                _logger.LogInfo("[Winget] winget is available in PATH.");
                return;
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string fullPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", "winget.exe");
            if (File.Exists(fullPath))
            {
                _wingetPath = fullPath;
                _pathResolved = true;
                _logger.LogInfo($"[Winget] winget found at local path: {fullPath}");
            }
            else
            {
                _logger.LogWarning("[Winget] winget.exe not found in PATH or default local path.");
            }
        }

        private void LogFiltered(string line, string context)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            // Filter out spinners: - \ | /
            string trimmed = line.Trim();
            if (trimmed == "-" || trimmed == "\\" || trimmed == "|" || trimmed == "/") return;

            // Filter out progress bar characters (Winget uses blocks)
            // Common block characters in different encodings: █ (U+2588) often appears as Γûê in some locales
            if (line.Contains("Γûê") || line.Contains("ΓûÆ") || line.Contains("█") || line.Contains("░")) 
            {
                // Only log if it's the final 100% or similar meaningful update (optional)
                // For now, let's just suppress the spammy progress lines
                return;
            }

            _logger.LogInfo($"[Winget:{context}] {line}");
        }

        private void UpdateSource(bool dryRun)
        {
            if (_sourceUpdated || dryRun) return;

            _logger.LogInfo("[Winget] Updating package sources...");
            _processRunner.RunCommand(_wingetPath, "source update", false, line => LogFiltered(line, "SourceUpdate"));
            _sourceUpdated = true;
        }

        public bool IsAvailable()
        {
            ResolveWingetPath();
            return Bootstrapper.IsInstalled();
        }

        public void Install(AppConfig app, bool dryRun)
        {
            ResolveWingetPath();
            
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

            UpdateSource(dryRun);

            _logger.LogInfo($"[Winget] Installing {app.Id}...");
            string args = $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements --disable-interactivity --no-upgrade";
            if (!string.IsNullOrEmpty(app.Source)) args += $" --source {app.Source}";

            bool alreadyInstalled = false;
            bool success = _processRunner.RunCommand(_wingetPath, args, false, line => 
            {
                LogFiltered(line, "Install");
                if (line != null && line.Contains("A package version is already installed", StringComparison.OrdinalIgnoreCase))
                {
                    alreadyInstalled = true;
                }
            });

            if (!success)
            {
                if (alreadyInstalled)
                {
                    _logger.LogSuccess($"[Success] {app.Id} is already installed (detected during install attempt).");
                    return;
                }
                throw new Exception($"Failed to install {app.Id} using Winget.");
            }
            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            ResolveWingetPath();
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Winget");
                return;
            }

            _logger.LogInfo($"[Winget] Uninstalling {appId}...");
            string args = $"uninstall --id {appId} -e --silent --accept-source-agreements --disable-interactivity";

            if (!_processRunner.RunCommand(_wingetPath, args, false, line => LogFiltered(line, "Uninstall")))
            {
                throw new Exception($"Failed to uninstall {appId} using Winget.");
            }
            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            ResolveWingetPath();
            var output = _processRunner.RunCommandWithOutput(_wingetPath, $"list -q {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}