using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services;

namespace WinHome
{
    public class Engine
    {
        // We store our tools in a Dictionary for fast lookup
        private readonly Dictionary<string, IPackageManager> _managers;
        private const string StateFileName = "winhome.state.json";

        public Engine()
        {
            // Register available managers
            _managers = new Dictionary<string, IPackageManager>(StringComparer.OrdinalIgnoreCase)
            {
                { "winget", new WingetService() },
                { "choco", new ChocolateyService() }
            };
        }

        public void Run(Configuration config)
        {
            Console.WriteLine($"--- WinHome v{config.Version} ---");

            // 1. Load State
            var previousApps = LoadState();
            var currentApps = config.Apps.Select(a => $"{a.Manager}:{a.Id}").ToHashSet();

            // 2. Cleanup (Uninstall removed apps)
            var appsToRemove = previousApps.Except(currentApps).ToList();
            if (appsToRemove.Any())
            {
                Console.WriteLine("\n--- Cleaning Up ---");
                foreach (var uniqueId in appsToRemove)
                {
                    // uniqueId format is "manager:appId"
                    var parts = uniqueId.Split(':', 2);
                    if (parts.Length == 2 && _managers.TryGetValue(parts[0], out var mgr))
                    {
                        mgr.Uninstall(parts[1]);
                    }
                }
            }

            // 3. Install/Reconcile
            if (config.Apps.Any())
            {
                Console.WriteLine("\n--- Reconciling Apps ---");
                foreach (var app in config.Apps)
                {
                    if (_managers.TryGetValue(app.Manager, out var mgr))
                    {
                        // Optional: Check if the manager (e.g. Choco) is actually installed on the PC first
                        if (!mgr.IsAvailable())
                        {
                            Console.WriteLine($"[Error] Package manager '{app.Manager}' is not installed on this system.");
                            continue;
                        }
                        
                        mgr.Install(app);
                    }
                    else
                    {
                        Console.WriteLine($"[Error] Unknown package manager: {app.Manager}");
                    }
                }
            }

            // 4. Save State
            SaveState(currentApps);
            Console.WriteLine("\n[State Saved] Configuration synced.");
        }

        private void SaveState(HashSet<string> apps)
        {
            try 
            {
                string json = JsonSerializer.Serialize(apps, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFileName, json);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"[Warning] Could not save state: {ex.Message}");
            }
        }

        private HashSet<string> LoadState()
        {
            if (!File.Exists(StateFileName))
            {
                return new HashSet<string>();
            }

            try 
            {
                string json = File.ReadAllText(StateFileName);
                return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            }
            catch 
            {
                return new HashSet<string>();
            }
        }
    }
}