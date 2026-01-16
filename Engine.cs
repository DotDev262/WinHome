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
        private const string StateFileName = "winhome.state.json";
        private readonly object _consoleLock = new();

        public Engine(
            Dictionary<string, IPackageManager> managers,
            IDotfileService dotfiles,
            IRegistryService registry,
            ISystemSettingsService systemSettings,
            IWslService wsl,
            IGitService git,
            IEnvironmentService env,
            IWindowsServiceManager serviceManager,
            IScheduledTaskService scheduledTaskService)
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
        }

        public async Task RunAsync(Configuration config, bool dryRun, string? profileName = null, bool debug = false)
        {
            Console.WriteLine($"--- WinHome v{config.Version} ---");

            if (!string.IsNullOrEmpty(profileName))
            {
                if (config.Profiles != null && config.Profiles.TryGetValue(profileName, out var profile))
                {
                    Console.WriteLine($"\n[Profile] Activating '{profileName}'...");
                    if (profile.Git != null) config.Git = profile.Git;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Error] Profile '{profileName}' not found.");
                    Console.ResetColor();
                    if (!dryRun) return;
                }
            }

            // Interface Calls (Mockable)
            var presetTweaks = await _systemSettings.GetTweaksAsync(config.SystemSettings);
            var allTweaks = config.RegistryTweaks.Concat(presetTweaks).ToList();
            
            var previousState = LoadState();
            var currentState = new HashSet<string>();
            
            foreach(var app in config.Apps) currentState.Add($"{app.Manager}:{app.Id}");
            foreach(var reg in allTweaks) currentState.Add($"reg:{reg.Path}|{reg.Name}");

            // Cleanup
            var itemsToRemove = previousState.Except(currentState).ToList();
            if (itemsToRemove.Any())
            {
                Console.WriteLine("\n--- Cleaning Up ---");
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
                Console.WriteLine("\n--- Reconciling Apps ---");
                await Task.Run(() => Parallel.ForEach(config.Apps, app =>
                {
                    if (_managers.TryGetValue(app.Manager, out var mgr))
                    {
                        if (!mgr.IsAvailable())
                        {
                            lock(_consoleLock)
                            {
                                Console.WriteLine($"[Error] Manager '{app.Manager}' not found.");
                            }
                            return;
                        }
                        mgr.Install(app, dryRun);
                    }
                    else
                    {
                        lock(_consoleLock)
                        {
                            Console.WriteLine($"[Error] Unknown manager: {app.Manager}");
                        }
                    }
                }));
            }

            if (config.Git != null) _git.Configure(config.Git, dryRun);
            
            if (config.Wsl != null) 
            {
                Console.WriteLine("\n--- Configuring WSL ---");
                _wsl.Configure(config.Wsl, dryRun);
            }
            
            if (config.EnvVars.Any())
            {
                Console.WriteLine("\n--- Configuring Environment Variables ---");
                await Task.Run(() => Parallel.ForEach(config.EnvVars, env => _env.Apply(env, dryRun)));
            }

            if (allTweaks.Any() && OperatingSystem.IsWindows())
            {
                Console.WriteLine("\n--- Applying Registry Tweaks ---");
                await Task.Run(() => Parallel.ForEach(allTweaks, tweak => _registry.Apply(tweak, dryRun)));
            }

            if (config.Dotfiles.Any())
            {
                Console.WriteLine("\n--- Linking Dotfiles ---");
                await Task.Run(() => Parallel.ForEach(config.Dotfiles, dotfile => _dotfiles.Apply(dotfile, dryRun)));
            }

            if (config.Services.Any())
            {
                Console.WriteLine("\n--- Managing Windows Services ---");
                await Task.Run(() => Parallel.ForEach(config.Services, service => _serviceManager.Apply(service, dryRun)));
            }

            if (config.ScheduledTasks.Any())
            {
                Console.WriteLine("\n--- Scheduling Tasks ---");
                await Task.Run(() => Parallel.ForEach(config.ScheduledTasks, task => _scheduledTaskService.Apply(task, dryRun)));
            }

            if (!dryRun)
            {
                SaveState(currentState);
                Console.WriteLine("\n[State Saved] Configuration synced.");
            }
            else
            {
                Console.WriteLine("\n[Dry Run] State was NOT saved.");
            }
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
                Console.WriteLine($"[Warning] Could not save state: {ex.Message}");
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