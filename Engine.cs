using System.Text.Json; // Used to save the state file
using WinHome.Models;
using WinHome.Services;

namespace WinHome
{
    public class Engine
    {
        private readonly WingetService _winget;
        private const string StateFileName = "winhome.state.json";

        public Engine()
        {
            _winget = new WingetService();
        }

        public void Run(Configuration config)
        {
            Console.WriteLine($"--- WinHome v{config.Version} ---");

            // 1. Load the "Memory" (Previous State)
            HashSet<string> previousApps = LoadState();
            
            // 2. Get the "Goal" (Current Config)
            HashSet<string> currentApps = config.Apps.Select(a => a.Id).ToHashSet();

            // 3. Calculate "Removals" (In Memory but NOT in Goal)
            var appsToRemove = previousApps.Except(currentApps).ToList();
            
            if (appsToRemove.Any())
            {
                Console.WriteLine("\n--- Cleaning Up ---");
                foreach (var appId in appsToRemove)
                {
                    _winget.Uninstall(appId);
                }
            }

            // 4. Calculate "Installs" (In Goal)
            if (config.Apps.Any())
            {
                Console.WriteLine("\n--- Reconciling Apps ---");
                foreach (var app in config.Apps)
                {
                    _winget.EnsureInstalled(app);
                }
            }

            // 5. Save the new State
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