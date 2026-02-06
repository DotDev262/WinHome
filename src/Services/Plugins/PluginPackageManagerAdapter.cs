using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Models.Plugins;

namespace WinHome.Services.Plugins
{
    public class PluginPackageManagerAdapter : IPackageManager
    {
        private readonly PluginManifest _plugin;
        private readonly IPluginRunner _runner;
        private readonly IPluginManager _manager;
        private readonly IRuntimeResolver _resolver;

        public PluginPackageManagerAdapter(PluginManifest plugin, IPluginRunner runner, IPluginManager manager, IRuntimeResolver resolver)
        {
            _plugin = plugin;
            _runner = runner;
            _manager = manager;
            _resolver = resolver;
        }

        public IPackageManagerBootstrapper Bootstrapper => new PluginRuntimeBootstrapper(_plugin, _manager, _resolver);

        public bool IsAvailable()
        {
            // For a plugin, "Available" means the plugin file exists and runtime is ready.
            return Bootstrapper.IsInstalled();
        }

        public bool IsInstalled(string appId)
        {
            var result = _runner.ExecuteAsync(_plugin, "check_installed", new { packageId = appId }, null).Result;
            return result.Success && result.Data?.ToString()?.ToLower() == "true";
        }

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
