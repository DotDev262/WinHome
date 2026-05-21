using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;

namespace WinHome.Services.Managers
{
    /// <summary>
    /// Provides an implementation of the package manager service using the Windows Package Manager (Winget) 
    /// toolset to discover, install, upgrade, and remove software application packages.
    /// </summary>
    public class WingetService : IPackageManager
    {
        private string _wingetPath = "winget";
        private bool _pathResolved = false;
        private bool _sourceUpdated = false;
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        private readonly IRuntimeResolver _resolver;

        /// <summary>
        /// Gets the bootstrapper instance responsible for evaluating or deploying the underlying Winget execution environment components.
        /// </summary>
        public IPackageManagerBootstrapper Bootstrapper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WingetService"/> class with required system automation dependencies.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute command-line operations.</param>
        /// <param name="bootstrapper">The tool deployment bootstrapper specific to managing the Winget subsystem environment lifecycle.</param>
        /// <param name="logger">The diagnostic logging service instance used to output status and event details.</param>
        /// <param name="resolver">The environment runtime configuration resolver utilized to map application executable entry points.</param>
        public WingetService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger, IRuntimeResolver resolver)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
            _resolver = resolver;
        }

        private void ResolveWingetPath()
        {
            if (_pathResolved && _wingetPath != "winget") return;
            _wingetPath = _resolver.Resolve("winget");
            _pathResolved = true;
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

        /// <summary>
        /// Evaluates whether the underlying Windows Package Manager command core path and runtime are available for application tracking tasks.
        /// </summary>
        /// <returns><c>true</c> if the package manager engine is verified as installed; otherwise, <c>false</c>.</returns>
        public bool IsAvailable()
        {
            ResolveWingetPath();
            return Bootstrapper.IsInstalled();
        }

        /// <summary>
        /// Deploys a specified application package using automated command options directed to the local Winget execution runtime.
        /// </summary>
        /// <param name="app">The configuration model data denoting the identification parameters of the targeted package to install.</param>
        /// <param name="dryRun">If set to <c>true</c>, simulates the package download and deployment steps without writing environmental changes.</param>
        /// <exception cref="Exception">Thrown if an unexpected operational failure occurs during process execution or exit diagnostics fail.</exception>
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

        /// <summary>
        /// Uninstalls a specified application package matching the exact program identifier from the system using quiet operational modes.
        /// </summary>
        /// <param name="appId">The unique string identity representing the tracking package descriptor to remove.</param>
        /// <param name="dryRun">If set to <c>true</c>, performs an operational simulation check without making permanent host updates.</param>
        /// <exception cref="Exception">Thrown if the application removal process errors out or fails confirmation routines.</exception>
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

        /// <summary>
        /// Queries the local Windows package tracking index database to check if the app identifier code exists in the active tracking dictionary list.
        /// </summary>
        /// <param name="appId">The target application string identity identifier to lookup.</param>
        /// <returns><c>true</c> if the targeted local package match string exists within the repository response output; otherwise, <c>false</c>.</returns>
        public bool IsInstalled(string appId)
        {
            ResolveWingetPath();
            var output = _processRunner.RunCommandWithOutput(_wingetPath, $"list -q {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}