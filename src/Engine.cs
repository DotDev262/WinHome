using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome
{
    public class Engine
    {
        // Dependencies are now Interfaces (Mockable)
        private readonly Dictionary<string, IPackageManager> _managers;
        private readonly IDotfileService _dotfiles;
        private readonly IRegistryService _registry;
        private readonly ISystemSettingsService _systemSettings;
        private readonly IWslService _wsl;
        private readonly IGitService _git;
        private readonly IEnvironmentService _env;
        private readonly IWindowsServiceManager _serviceManager;
        private readonly IScheduledTaskService _scheduledTaskService;
        private readonly ILogger _logger;
        private readonly IPluginManager _pluginManager;
        private readonly IPluginRunner _pluginRunner;
        private readonly IStateService _stateService;
        private readonly IRuntimeResolver _runtimeResolver;

        public Engine(
            Dictionary<string, IPackageManager> managers,
            IDotfileService dotfiles,
            IRegistryService registry,
            ISystemSettingsService systemSettings,
            IWslService wsl,
            IGitService git,
            IEnvironmentService env,
            IWindowsServiceManager serviceManager,
            IScheduledTaskService scheduledTaskService,
            IPluginManager pluginManager,
            IPluginRunner pluginRunner,
            IStateService stateService,
            ILogger logger,
            IRuntimeResolver runtimeResolver)
        {
            _managers = managers;
            _dotfiles = dotfiles;
            _registry = registry;
            _systemSettings = systemSettings;
            _wsl = wsl;
            _git = git;
            _env = env;
            _serviceManager = serviceManager;
            _scheduledTaskService = scheduledTaskService;
            _pluginManager = pluginManager;
            _pluginRunner = pluginRunner;
            _stateService = stateService;
            _logger = logger;
            _runtimeResolver = runtimeResolver;
        }

        public async Task RunAsync(Configuration config, bool dryRun, string? profileName = null, bool debug = false, bool diff = false)
        {
            _logger.LogInfo($"--- WinHome v{config.Version} ---");

            // Load Plugins
            var plugins = _pluginManager.DiscoverPlugins().ToList();
            var loggedPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var plugin in plugins)
            {
                if (plugin.Capabilities.Contains("package_manager"))
                {
                    // Register the plugin as a package manager
                    if (!_managers.ContainsKey(plugin.Name))
                    {
                        // Delay logging discovery until it's actually used by an app
                        _managers[plugin.Name] = new WinHome.Services.Plugins.PluginPackageManagerAdapter(plugin, _pluginRunner, _pluginManager, _runtimeResolver);
                    }
                }
            }

            if (!string.IsNullOrEmpty(profileName))
            {
                if (config.Profiles != null && config.Profiles.TryGetValue(profileName, out var profile))
                {
                    _logger.LogInfo($"\n[Profile] Activating '{profileName}'...");
                    if (profile.Git != null) config.Git = profile.Git;
                }
                else
                {
                    _logger.LogError($"[Error] Profile '{profileName}' not found.");
                    if (!dryRun) return;
                }
            }

            if (diff)
            {
                await PrintDiffAsync(config);
                return;
            }

            // Check network if we have apps to install or WSL update enabled
            if ((config.Apps.Any() || (config.Wsl != null && config.Wsl.Update)) && !dryRun)
            {
                if (!WaitForNetwork())
                {
                    _logger.LogWarning("[Warning] No internet connection detected. Package manager operations may fail.");
                }
            }

            var currentState = await BuildStateFromConfig(config);

            var previousState = _stateService.LoadState();

            // Cleanup
            var itemsToRemove = previousState.Except(currentState).ToList();
            if (itemsToRemove.Any())
            {
                _logger.LogInfo("\n--- Cleaning Up ---");
                await Task.Run(() => Parallel.ForEach(itemsToRemove, uniqueId =>
                {
                    if (uniqueId.StartsWith("reg:"))
                    {
                        var parts = uniqueId.Substring(4).Split('|', 2);
                        if (parts.Length == 2) _registry.Revert(parts[0], parts[1], dryRun);
                    }
                    else
                    {
                        var parts = uniqueId.Split(':', 2);
                        if (parts.Length == 2 && _managers.TryGetValue(parts[0], out var mgr))
                        {
                            mgr.Uninstall(parts[1], dryRun);
                        }
                    }
                }));
            }

            // 1. Ensure System Managers (Scoop) are ready if needed by plugins
            if (plugins.Any(p => p.Type.ToLower() == "python" || p.Type.ToLower() == "javascript" || p.Type.ToLower() == "typescript"))
            {
                if (_managers.TryGetValue("scoop", out var scoopMgr))
                {
                    if (!scoopMgr.IsAvailable())
                    {
                        _logger.LogInfo("\n--- Bootstrapping System Managers ---");
                        _logger.LogInfo("[Engine] Bootstrapping Scoop for plugin runtimes...");
                        scoopMgr.Bootstrapper.Install(dryRun);
                    }
                }
            }

            // 2. Reconcile Plugin Runtimes
            var pluginsNeedingRuntime = plugins.Where(p => !p.Type.Equals("executable", StringComparison.OrdinalIgnoreCase)).ToList();
            if (pluginsNeedingRuntime.Any())
            {
                // Only reconcile runtimes for plugins that are actually used in the config
                var usedPluginNames = config.Apps.Select(a => a.Manager)
                    .Concat(new[] { "vim", "vscode" }.Where(_ => config.Vim != null || config.Vscode != null))
                    .Concat(config.Extensions.Keys)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var usedPluginsNeedingRuntime = pluginsNeedingRuntime.Where(p => usedPluginNames.Contains(p.Name)).ToList();

                if (usedPluginsNeedingRuntime.Any())
                {
                    _logger.LogInfo("\n--- Reconciling Plugin Runtimes ---");
                    foreach (var plugin in usedPluginsNeedingRuntime)
                    {
                        await _pluginManager.EnsureRuntimeAsync(plugin);
                    }
                    _env.RefreshPath();
                }
            }

            // Install Apps
            if (config.Apps.Any())
            {
                _logger.LogInfo("\n--- Reconciling Apps ---");
                foreach (var app in config.Apps)
                {
                    _logger.LogInfo($"[Engine] Processing {app.Manager}:{app.Id}...");
                    if (_managers.TryGetValue(app.Manager, out var mgr))
                    {
                        if (mgr is WinHome.Services.Plugins.PluginPackageManagerAdapter adapter)
                        {
                             if (loggedPlugins.Add(app.Manager))
                             {
                                 _logger.LogInfo($"[Plugin] Discovered: {app.Manager} ({adapter.PluginType})");
                             }
                        }

                        if (!mgr.IsAvailable())
                        {
                            _logger.LogInfo($"[Engine] Manager '{app.Manager}' not available. Bootstrapping...");
                            mgr.Bootstrapper.Install(dryRun);
                            if (!mgr.IsAvailable())
                            {
                                _logger.LogError($"[Error] Manager '{app.Manager}' not found after attempting to install it.");
                                continue;
                            }
                        }
                        mgr.Install(app, dryRun);
                        _env.RefreshPath();
                    }
                    else
                    {
                        _logger.LogError($"[Error] Unknown manager: {app.Manager}");
                    }
                }
            }

            if (config.Git != null) _git.Configure(config.Git, dryRun);

            if (config.Wsl != null)
            {
                _logger.LogInfo("\n--- Configuring WSL ---");
                _wsl.Configure(config.Wsl, dryRun);
            }

            if (config.EnvVars.Any())
            {
                _logger.LogInfo("\n--- Configuring Environment Variables ---");
                await Task.Run(() => Parallel.ForEach(config.EnvVars, env => _env.Apply(env, dryRun)));
            }

            // Plugin Extensions
            var allExtensions = new Dictionary<string, object>(config.Extensions);
            if (config.Vim != null) allExtensions["vim"] = config.Vim;
            if (config.Vscode != null) allExtensions["vscode"] = config.Vscode;

            if (allExtensions.Any())
            {
                _logger.LogInfo("\n--- Running Plugin Extensions ---");
                foreach (var ext in allExtensions)
                {
                    var pluginName = ext.Key;
                    var pluginConfig = ext.Value;

                    // Find plugin by name
                    var plugin = plugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                    
                    if (plugin != null)
                    {
                        if (loggedPlugins.Add(plugin.Name))
                        {
                             _logger.LogInfo($"[Plugin] Discovered: {plugin.Name} ({plugin.Type})");
                        }
                        
                        await _pluginManager.EnsureRuntimeAsync(plugin);
                        _logger.LogInfo($"[Plugin] Applying configuration for '{pluginName}'...");
                        var result = await _pluginRunner.ExecuteAsync(plugin, "apply", pluginConfig, new { dryRun = dryRun });
                        
                        if (!result.Success)
                        {
                            _logger.LogError($"[Error] Plugin '{pluginName}' failed: {result.Error}");
                        }
                        else if (result.Changed)
                        {
                            _logger.LogSuccess($"[Plugin] '{pluginName}' applied successfully.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"[Warning] Configuration found for '{pluginName}' but no matching plugin is installed.");
                    }
                }
            }

            var presetTweaks = await _systemSettings.GetTweaksAsync(config.SystemSettings);
            var allTweaks = config.RegistryTweaks.Concat(presetTweaks).ToList();

            if (allTweaks.Any() && OperatingSystem.IsWindows())
            {
                _logger.LogInfo("\n--- Applying Registry Tweaks ---");
                await Task.Run(() => Parallel.ForEach(allTweaks, tweak => _registry.Apply(tweak, dryRun)));
            }

            if (config.SystemSettings.Any() && OperatingSystem.IsWindows())
            {
                _logger.LogInfo("\n--- Applying System Settings ---");
                await _systemSettings.ApplyNonRegistrySettingsAsync(config.SystemSettings, dryRun);
            }

            if (config.Dotfiles.Any())
            {
                _logger.LogInfo("\n--- Linking Dotfiles ---");
                await Task.Run(() => Parallel.ForEach(config.Dotfiles, dotfile => _dotfiles.Apply(dotfile, dryRun)));
            }

            if (config.Services.Any())
            {
                _logger.LogInfo("\n--- Managing Windows Services ---");
                await Task.Run(() => Parallel.ForEach(config.Services, service => _serviceManager.Apply(service, dryRun)));
            }

            if (config.ScheduledTasks.Any())
            {
                _logger.LogInfo("\n--- Scheduling Tasks ---");
                await Task.Run(() => Parallel.ForEach(config.ScheduledTasks, task => _scheduledTaskService.Apply(task, dryRun)));
            }

            if (!dryRun)
            {
                _stateService.SaveState(currentState);
                _logger.LogSuccess("\n[State Saved] Configuration synced.");
            }
            else
            {
                _logger.LogWarning("\n[Dry Run] State was NOT saved.");
            }
        }

        public async Task PrintDiffAsync(Configuration config)
        {
            _logger.LogInfo("\n--- State Diff ---");

            var previousState = _stateService.LoadState();
            var currentState = await BuildStateFromConfig(config);

            var itemsToRemove = previousState.Except(currentState).ToList();
            var itemsToAdd = currentState.Except(previousState).ToList();
            var unchangedItems = previousState.Intersect(currentState).ToList();

            if (!itemsToRemove.Any() && !itemsToAdd.Any())
            {
                _logger.LogSuccess("No changes detected. System is up to date.");
                return;
            }

            if (itemsToRemove.Any())
            {
                _logger.LogError("\n[-] Items to Remove:");
                foreach (var item in itemsToRemove)
                {
                    _logger.LogError($"  - {item}");
                }
            }

            if (itemsToAdd.Any())
            {
                _logger.LogSuccess("\n[+] Items to Add:");
                foreach (var item in itemsToAdd)
                {
                    _logger.LogSuccess($"  + {item}");
                }
            }

            if (unchangedItems.Any())
            {
                _logger.LogInfo("\n[=] Unchanged Items:");
                foreach (var item in unchangedItems)
                {
                    _logger.LogInfo($"  = {item}");
                }
            }
        }

        private async Task<HashSet<string>> BuildStateFromConfig(Configuration config)
        {
            var state = new HashSet<string>();

            // App managers
            foreach (var app in config.Apps)
            {
                state.Add($"{app.Manager}:{app.Id}");
            }

            // Registry tweaks
            var presetTweaks = await _systemSettings.GetTweaksAsync(config.SystemSettings);
            var allTweaks = config.RegistryTweaks.Concat(presetTweaks).ToList();
            foreach (var reg in allTweaks)
            {
                state.Add($"reg:{reg.Path}|{reg.Name}");
            }

            return state;
        }

        private bool WaitForNetwork(int timeoutSeconds = 30)
        {
            _logger.LogInfo("[Engine] Checking for internet connectivity...");
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    using var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send("1.1.1.1", 2000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        _logger.LogSuccess("[Engine] Internet connection verified.");
                        return true;
                    }
                }
                catch { }
                
                _logger.LogInfo("[Engine] Waiting for network...");
                Thread.Sleep(2000);
            }
            return false;
        }
    }
}
