using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;

namespace WinHome.Services.Managers
{
    /// <summary>
    /// Provides an implementation of the package manager service using Chocolatey to orchestrate 
    /// application installation, uninstallation, and state verification on the host machine.
    /// </summary>
    public class ChocolateyService : IPackageManager
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        private readonly IRuntimeResolver _resolver;

        /// <summary>
        /// Gets the bootstrapper instance responsible for evaluating or deploying the underlying Chocolatey installation requirements.
        /// </summary>
        public IPackageManagerBootstrapper Bootstrapper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChocolateyService"/> class with necessary sub-system execution dependencies.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute command-line operations.</param>
        /// <param name="bootstrapper">The tool deployment bootstrapper specific to managing the Chocolatey application lifecycle.</param>
        /// <param name="logger">The diagnostic logging service instance used to output status and event details.</param>
        /// <param name="resolver">The environment runtime configuration resolver utilized to map application executable entry points.</param>
        public ChocolateyService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger, IRuntimeResolver resolver)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
            _logger = logger;
            _resolver = resolver;
        }

        private string GetChocoExecutable()
        {
            return _resolver.Resolve("choco");
        }

        /// <summary>
        /// Evaluates whether the underlying Chocolatey engine runtime environment is ready and verified for use on the system.
        /// </summary>
        /// <returns><c>true</c> if the package manager engine is verified as installed; otherwise, <c>false</c>.</returns>
        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        private void LogFiltered(string line, string context)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (line.Contains("Progress: Downloading")) return;
            _logger.LogInfo($"[Choco:{context}] {line}");
        }

        /// <summary>
        /// Deploys a specified application package through the Chocolatey service execution wrapper.
        /// </summary>
        /// <param name="app">The configuration model data denoting the identification parameters of the targeted package to install.</param>
        /// <param name="dryRun">If set to <c>true</c>, simulates the package download and deployment steps without writing environmental changes.</param>
        /// <exception cref="Exception">Thrown if an unexpected operational failure occurs during process execution or exit diagnostics fail.</exception>
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

            bool alreadyInstalled = false;
            bool success = _processRunner.RunCommand(executable, args, false, line =>
            {
                LogFiltered(line, "Install");
                // Chocolatey sometimes returns non-zero even if packages are technically present or if it just says "0/1 packages installed"
                if (line != null && (line.Contains("already installed", StringComparison.OrdinalIgnoreCase) || line.Contains("packages installed currently", StringComparison.OrdinalIgnoreCase)))
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
                throw new Exception($"Failed to install {app.Id} using Chocolatey.");
            }

            _logger.LogSuccess($"[Success] Installed {app.Id}");
        }

        /// <summary>
        /// Uninstalls a specified application package by executing the Chocolatey removal script commands.
        /// </summary>
        /// <param name="appId">The unique string identity representing the tracking package descriptor to remove.</param>
        /// <param name="dryRun">If set to <c>true</c>, performs an operational simulation check without making permanent host updates.</param>
        /// <exception cref="Exception">Thrown if the application removal process errors out or fails confirmation routines.</exception>
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

            if (!_processRunner.RunCommand(executable, args, false, line => LogFiltered(line, "Uninstall")))
            {
                throw new Exception($"Failed to uninstall {appId} using Chocolatey.");
            }

            _logger.LogSuccess($"[Success] Uninstalled {appId}");
        }

        /// <summary>
        /// Queries the local Chocolatey package database registry to verify if the specified tracking identifier is currently installed.
        /// </summary>
        /// <param name="appId">The target application string identity identifier to lookup.</param>
        /// <returns><c>true</c> if the targeted local package match string exists within the repository response output; otherwise, <c>false</c>.</returns>
        public bool IsInstalled(string appId)
        {
            string executable = GetChocoExecutable();
            string output = _processRunner.RunCommandWithOutput(executable, $"list -l -r {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}