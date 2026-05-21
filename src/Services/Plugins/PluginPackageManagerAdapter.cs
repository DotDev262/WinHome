using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Models.Plugins;

namespace WinHome.Services.Plugins
{
    /// <summary>
    /// Acts as an adapter that bridges an underlying extensibility plugin manifest execution workflow 
    /// into a unified, system-native <see cref="IPackageManager"/> engine integration wrapper.
    /// </summary>
    public class PluginPackageManagerAdapter : IPackageManager
    {
        private readonly PluginManifest _plugin;
        private readonly IPluginRunner _runner;
        private readonly IPluginManager _manager;
        private readonly IRuntimeResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginPackageManagerAdapter"/> class mapping specified plugin settings to system actions.
        /// </summary>
        /// <param name="plugin">The manifest context metadata configuration tracking information for the target adapter plugin.</param>
        /// <param name="runner">The cross-platform script environment runner pipeline used to dispatch commands.</param>
        /// <param name="manager">The central plugin sub-system coordinator manager used to ensure execution runtimes.</param>
        /// <param name="resolver">The environment configuration path variable resolver wrapper instance.</param>
        public PluginPackageManagerAdapter(PluginManifest plugin, IPluginRunner runner, IPluginManager manager, IRuntimeResolver resolver)
        {
            _plugin = plugin;
            _runner = runner;
            _manager = manager;
            _resolver = resolver;
        }

        /// <summary>
        /// Gets the string classification specifying the targeted execution runtime environment type needed by the plugin.
        /// </summary>
        public string PluginType => _plugin.Type;

        /// <summary>
        /// Gets an interface implementation instance dedicated to configuring, evaluating, or installing runtime dependencies for this plugin.
        /// </summary>
        public IPackageManagerBootstrapper Bootstrapper => new PluginRuntimeBootstrapper(_plugin, _manager, _resolver);

        /// <summary>
        /// Verifies whether the targeted plugin manifest file is valid and its structural execution runtimes are ready on the host system.
        /// </summary>
        /// <returns><c>true</c> if the adapted plugin bootstrapper detects a validated operational status; otherwise, <c>false</c>.</returns>
        public bool IsAvailable()
        {
            // For a plugin, "Available" means the plugin file exists and runtime is ready.
            return Bootstrapper.IsInstalled();
        }

        /// <summary>
        /// Communicates with the adapted plugin execution script to query whether the specified tracking identity is present on the machine.
        /// </summary>
        /// <param name="appId">The unique target package string identifier profile lookup to check.</param>
        /// <returns><c>true</c> if the plugin runner confirms a successful installation match status; otherwise, <c>false</c>.</returns>
        public bool IsInstalled(string appId)
        {
            var result = _runner.ExecuteAsync(_plugin, "check_installed", new { packageId = appId }, null).Result;
            return result.Success && result.Data?.ToString()?.ToLower() == "true";
        }

        /// <summary>
        /// Executes an automated installation request by dispatching a structured action event argument payload to the plugin runner context.
        /// </summary>
        /// <param name="app">The application target configuration model data container containing installation arguments.</param>
        /// <param name="dryRun">If set to <c>true</c>, passes a simulation block state instruction flags context to the execution handler script.</param>
        /// <exception cref="Exception">Thrown if the underlying script execution engine returns a failing response indicator or errors out.</exception>
        public void Install(AppConfig app, bool dryRun)
        {
            // Ensure runtime is available before execution
            _manager.EnsureRuntimeAsync(_plugin).Wait();

            var args = new
            {
                packageId = app.Id,
                version = app.Version,
                @params = app.Params
            };

            var context = new { dryRun = dryRun };

            var result = _runner.ExecuteAsync(_plugin, "install", args, context).Result;

            if (!result.Success)
            {
                throw new Exception($"Plugin '{_plugin.Name}' failed to install '{app.Id}': {result.Error}");
            }
        }

        /// <summary>
        /// Requests a package removal execution routine by transmitting a structured request down to the adapted plugin tracking handler.
        /// </summary>
        /// <param name="appId">The tracking package match string name identity to remove.</param>
        /// <param name="dryRun">If set to <c>true</c>, forwards a simulation configuration flag context to the execution handler script.</param>
        /// <exception cref="Exception">Thrown if the script engine process reports uninstallation structural block failures.</exception>
        public void Uninstall(string appId, bool dryRun)
        {
            // Ensure runtime is available before execution
            _manager.EnsureRuntimeAsync(_plugin).Wait();

            var context = new { dryRun = dryRun };
            var result = _runner.ExecuteAsync(_plugin, "uninstall", new { packageId = appId }, context).Result;

            if (!result.Success)
            {
                // Log warning? For now throw.
                throw new Exception($"Plugin '{_plugin.Name}' failed to uninstall '{appId}': {result.Error}");
            }
        }

        // Inner class to satisfy the Interface contract
        private class PluginRuntimeBootstrapper : IPackageManagerBootstrapper
        {
            private readonly PluginManifest _p;
            private readonly IPluginManager _m;
            private readonly IRuntimeResolver _r;

            public PluginRuntimeBootstrapper(PluginManifest p, IPluginManager m, IRuntimeResolver r)
            {
                _p = p;
                _m = m;
                _r = r;
            }

            public string Name => $"{_p.Name} Runtime";

            public bool IsInstalled()
            {
                // We check if the required runtime is installed
                string exe = "";
                switch (_p.Type.ToLower())
                {
                    case "python":
                        exe = _r.Resolve("uv");
                        break;
                    case "typescript":
                    case "javascript":
                        exe = _r.Resolve("bun");
                        break;
                    case "executable":
                        return true;
                    default:
                        return true;
                }

                if (string.IsNullOrEmpty(exe)) return false;

                try
                {
                    return Process.Start(new ProcessStartInfo { FileName = exe, Arguments = "--version", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true })?.WaitForExit(1000) ?? false;
                }
                catch
                {
                    return false;
                }
            }

            public void Install(bool dryRun)
            {
                _m.EnsureRuntimeAsync(_p).Wait();
            }
        }
    }
}