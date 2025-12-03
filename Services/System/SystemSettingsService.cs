using WinHome.Models;

namespace WinHome.Services.System
{
    public class SystemSettingsService
    {
        private record SettingDefinition(
            string SettingKey,
            string RegistryPath,
            string RegistryName,
            string RegistryType,
            Dictionary<string, object> ValueMap
        );

        private readonly List<SettingDefinition> _catalog = new()
        {
            
            new("dark_mode", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "dword", 
                new() { { "true", 0 }, { "false", 1 } }),

            new("dark_mode", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", "dword", 
                new() { { "true", 0 }, { "false", 1 } }),

            
            new("taskbar_alignment", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", "dword", 
                new() { { "left", 0 }, { "center", 1 } }),

            new("taskbar_widgets", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", "dword", 
                new() { { "hide", 0 }, { "show", 1 } }),

            
            new("show_file_extensions", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", "dword", 
                new() { { "true", 0 }, { "false", 1 } }),

            new("show_hidden_files", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", "dword", 
                new() { { "true", 1 }, { "false", 2 } }),

            new("seconds_in_clock", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", "dword", 
                new() { { "true", 1 }, { "false", 0 } }),

            new("explorer_launch_to", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", "dword", 
                new() { { "this_pc", 1 }, { "quick_access", 2 } }),

            
            new("bing_search_enabled", 
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", "dword", 
                new() { { "true", 1 }, { "false", 0 } }),

            
        };

        public IEnumerable<RegistryTweak> GetTweaks(Dictionary<string, object> settings)
        {
            var tweaks = new List<RegistryTweak>();
            if (settings == null) return tweaks;

            foreach (var userSetting in settings)
            {
                string key = userSetting.Key.ToLower();
                string val = userSetting.Value.ToString()?.ToLower() ?? "";

                var matches = _catalog.Where(d => d.SettingKey == key);

                foreach (var def in matches)
                {
                    if (def.ValueMap.TryGetValue(val, out object? regValue))
                    {
                        tweaks.Add(new RegistryTweak 
                        { 
                            Path = def.RegistryPath, 
                            Name = def.RegistryName, 
                            Value = regValue, 
                            Type = def.RegistryType 
                        });
                    }
                    else
                    {
                        Console.WriteLine($"[Warning] Invalid value '{val}' for setting '{key}'. Allowed: {string.Join(", ", def.ValueMap.Keys)}");
                    }
                }
            }
            return tweaks;
        }
    }
}