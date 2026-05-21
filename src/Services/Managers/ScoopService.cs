using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;

namespace WinHome.Services.Managers
{
    /// <summary>
    /// Provides an implementation of the package manager service using Scoop to manage 
    /// command-line utility configurations, installations, and software removals.
    /// </summary>
    public class ScoopService : IPackageManager
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        private readonly IRuntimeResolver _resolver;

        /// <summary>
        /// Gets the bootstrapper instance responsible for evaluating or deploying the underlying Scoop infrastructure.
        /// </summary>
        public IPackageManagerBootstrapper Bootstrapper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScoopService"/> class with necessary sub-system dependencies.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute command-line operations.</param>
        /// <param name="bootstrapper">The tool deployment bootstrapper specific to managing the Scoop runtime life-cycle.</param>
        /// <param name="logger">The diagnostic logging service instance used to output status and event details.</param>
        /// <param name="resolver">The environment runtime configuration resolver utilized to map application executable entry points.</param>
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

        /// <summary>
        /// Evaluates whether the underlying Scoop core command engine is ready and verified for use on the system.
        /// </summary>
        /// <returns><c>true</c> if the package manager engine is verified as installed; otherwise, <c>false</c>.</returns>
        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        /// <summary>
        /// Deploys a specified application package through the Scoop service command-line interface runtime.
        /// </summary>
        /// <param name="app">The configuration model data denoting the identification parameters of the targeted package to install.</param>
        /// <param name="dryRun">If set to <c>true</c>, simulates the package download and deployment steps without writing environmental changes.</param>
        /// <exception cref="Exception">Thrown if an unexpected operational failure occurs during process execution or if the bucket manifest is missing.</exception>
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

        /// <summary>
        /// Uninstalls a specified application package by executing the Scoop removal commands.
        /// </summary>
        /// <param name="appId">The unique string identity representing the tracking package descriptor to remove.</param>
        /// <param name="dryRun">If set to <c>true</c>, performs an operational simulation check without making permanent host updates.</param>
        /// <exception cref="Exception">Thrown if the application removal process errors out or fails execution routines.</exception>
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

        /// <summary>
        /// Queries the local Scoop database manifest registry directory to verify if the tracking identifier package is actively present.
        /// </summary>
        /// <param name="appId">The target application string identity identifier to lookup.</param>
        /// <returns><c>true</c> if the targeted local package match string exists within the generated manifest list output; otherwise, <c>false</c>.</returns>
        public bool IsInstalled(string appId)
        {
            string executable = GetScoopExecutable();
            string output = _processRunner.RunCommandWithOutput(executable, "list");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}