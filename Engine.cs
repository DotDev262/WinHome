using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;
using WinHome.Services.System;

namespace WinHome
{
    public class Engine
    {
        private readonly Dictionary<string, IPackageManager> _managers;
        private readonly DotfileService _dotfiles;
        private readonly RegistryService _registry;
        private readonly SystemSettingsService _systemSettings;
        private readonly WslService _wsl;
        private readonly GitService _git; // 1. Define Service
        private const string StateFileName = "winhome.state.json";

        public Engine()
        {
            _dotfiles = new DotfileService();
            _registry = new RegistryService();
            _systemSettings = new SystemSettingsService();
            _wsl = new WslService();
            _git = new GitService(); // 2. Initialize Service

            _managers = new Dictionary<string, IPackageManager>(StringComparer.OrdinalIgnoreCase)
            {
                { "winget", new WingetService() },
                { "choco", new ChocolateyService() },
                { "scoop", new ScoopService() },
                { "mise", new MiseService() }
            };
        }

        // UPDATED: Accept profileName (nullable)
        public void Run(Configuration config, bool dryRun, string? profileName = null)
        {
            Console.WriteLine($"--- WinHome v{config.Version} ---");

            // 1. Profile Application (The --work thing)
            if (!string.IsNullOrEmpty(profileName))
            {
                if (config.Profiles != null && config.Profiles.TryGetValue(profileName, out var profile))
                {
                    Console.WriteLine($"\n[Profile] Activating '{profileName}'...");
                    
                    // Override Git Config if present in profile
                    if (profile.Git != null)
                    {
                        Console.WriteLine($"[Profile] Overriding Git configuration with '{profileName}' settings.");
                        config.Git = profile.Git;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Error] Profile '{profileName}' not found in configuration.");
                    Console.ResetColor();
                    if (!dryRun) return; // Stop if profile is missing (unless dry run)
                }
            }

            var presetTweaks = _systemSettings.GetTweaks(config.SystemSettings);
            var allTweaks = config.RegistryTweaks.Concat(presetTweaks).ToList();
            
            var previousState = LoadState();
            var currentState = new HashSet<string>();
            
            foreach(var app in config.Apps) 
                currentState.Add($"{app.Manager}:{app.Id}");
                
            foreach(var reg in allTweaks)
                currentState.Add($"reg:{reg.Path}|{reg.Name}");

            // 2. Cleanup
            var itemsToRemove = previousState.Except(currentState).ToList();
            if (itemsToRemove.Any())
            {
                Console.WriteLine("\n--- Cleaning Up ---");
                foreach (var uniqueId in itemsToRemove)
                {
                    if (uniqueId.StartsWith("reg:"))
                    {
                        var payload = uniqueId.Substring(4);
                        var parts = payload.Split('|', 2);
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
                }
            }

            // 3. Install Apps
            if (config.Apps.Any())
            {
                Console.WriteLine("\n--- Reconciling Apps ---");
                foreach (var app in config.Apps)
                {
                    if (_managers.TryGetValue(app.Manager, out var mgr))
                    {
                        if (!mgr.IsAvailable())
                        {
                            Console.WriteLine($"[Error] Manager '{app.Manager}' not found.");
                            continue;
                        }
                        mgr.Install(app, dryRun);
                    }
                    else
                    {
                        Console.WriteLine($"[Error] Unknown manager: {app.Manager}");
                    }
                }
            }

            // 4. Configure Git
            if (config.Git != null)
            {
                Console.WriteLine("\n--- Configuring Git ---");
                _git.Configure(config.Git, dryRun);
            }

            // 5. Configure WSL
            if (config.Wsl != null)
            {
                Console.WriteLine("\n--- Configuring WSL ---");
                _wsl.Configure(config.Wsl, dryRun);
            }

            // 6. Registry Tweaks
            if (allTweaks.Any())
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.WriteLine("\n--- Applying Registry Tweaks ---");
                    foreach (var tweak in allTweaks)
                    {
                        _registry.Apply(tweak, dryRun);
                    }
                }
            }

            // 7. Dotfiles
            if (config.Dotfiles.Any())
            {
                Console.WriteLine("\n--- Linking Dotfiles ---");
                foreach (var dotfile in config.Dotfiles)
                {
                    _dotfiles.Apply(dotfile, dryRun);
                }
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