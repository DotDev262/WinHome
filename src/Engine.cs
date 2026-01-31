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
        private const string StateFileName = "winhome.state.json";

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
            ILogger logger)
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
            _logger = logger;
        }

        public async Task RunAsync(Configuration config, bool dryRun, string? profileName = null, bool debug = false, bool diff = false)
        {
            _logger.LogInfo($"--- WinHome v{config.Version} ---");

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

            // Interface Calls (Mockable)
            var currentState = await BuildStateFromConfig(config);

            var previousState = LoadState();

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

            // Install Apps
            if (config.Apps.Any())
            {
                _logger.LogInfo("\n--- Reconciling Apps ---");
                var bootstrapLock = new object();
                await Task.Run(() => Parallel.ForEach(config.Apps, app =>
                {
                    if (_managers.TryGetValue(app.Manager, out var mgr))
                    {
                        if (!mgr.IsAvailable())
                        {
                            lock (bootstrapLock)
                            {
                                if (!mgr.IsAvailable())
                                {
                                    mgr.Bootstrapper.Install(dryRun);
                                    if (!mgr.IsAvailable())
                                    {
                                        _logger.LogError($"[Error] Manager '{app.Manager}' not found after attempting to install it.");
                                        return;
                                    }
                                }
                            }
                        }
                        mgr.Install(app, dryRun);
                    }
                    else
                    {
                        _logger.LogError($"[Error] Unknown manager: {app.Manager}");
                    }
                }));
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
                SaveState(currentState);
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

            var previousState = LoadState();
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

        private void SaveState(HashSet<string> state)
        {
            try
            {
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFileName, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Warning] Could not save state: {ex.Message}");
            }
        }

        private HashSet<string> LoadState()
        {
            if (!File.Exists(StateFileName)) return new HashSet<string>();
            try
            {
                string json = File.ReadAllText(StateFileName);
                return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            }
            catch { return new HashSet<string>(); }
        }
    }
}