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

        public PluginPackageManagerAdapter(PluginManifest plugin, IPluginRunner runner, IPluginManager manager)
        {
            _plugin = plugin;
            _runner = runner;
            _manager = manager;
        }

        // The bootstrapper logic is handled by the PluginManager (EnsureRuntimeAsync)
        // But IPackageManager interface requires a Bootstrapper property.
        // We can return a dummy or wrap the manager's logic.
        public IPackageManagerBootstrapper Bootstrapper => new PluginRuntimeBootstrapper(_plugin, _manager);

        public bool IsAvailable()
        {
            // For a plugin, "Available" means the plugin file exists and runtime is ready.
            // We'll optimistically assume yes if Bootstrapper.IsInstalled is true.
            return Bootstrapper.IsInstalled();
        }

        public bool IsInstalled(string appId)
        {
            var result = _runner.ExecuteAsync(_plugin, "check_installed", new { packageId = appId }, null).Result;
            return result.Success && result.Data?.ToString()?.ToLower() == "true";
        }

        public void Install(AppConfig app, bool dryRun)
        {
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
            public PluginRuntimeBootstrapper(PluginManifest p, IPluginManager m) { _p = p; _m = m; }
            
            public string Name => $"{_p.Name} Runtime";
            
            // This is a bit of a simplification. 
            // Realistically we should check if 'uv'/'bun' is actually there.
            public bool IsInstalled() => true; 
            
            public void Install(bool dryRun) 
            {
                _m.EnsureRuntimeAsync(_p).Wait();
            }
        }
    }
}
