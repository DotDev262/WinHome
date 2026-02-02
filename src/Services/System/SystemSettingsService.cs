using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IProcessRunner _processRunner;
        private readonly List<string> _nonRegistryKeys = new() { "brightness", "volume", "notification" };

        private readonly Dictionary<string, List<RegistryTweak>> _securityPresets = new()
        {
            ["baseline"] = new()
            {
                // Enable SmartScreen for apps and files
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost", Name = "EnableWebContentEvaluation", Value = 1, Type = "dword" },
                // Disable Autorun/Autoplay
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", Name = "NoDriveTypeAutoRun", Value = 255, Type = "dword" },
                // Disable LLMNR (Local Link Multicast Name Resolution)
                new RegistryTweak { Path = @"HKLM\Software\Policies\Microsoft\Windows NT\DNSClient", Name = "EnableMulticast", Value = 0, Type = "dword" }
            },
            ["strict"] = new()
            {
                // Include baseline
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost", Name = "EnableWebContentEvaluation", Value = 1, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", Name = "NoDriveTypeAutoRun", Value = 255, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\Software\Policies\Microsoft\Windows NT\DNSClient", Name = "EnableMulticast", Value = 0, Type = "dword" },
                
                // Disable Windows Script Host
                new RegistryTweak { Path = @"HKLM\Software\Microsoft\Windows Script Host\Settings", Name = "Enabled", Value = 0, Type = "dword" },
                // Disable Remote Assistance
                new RegistryTweak { Path = @"HKLM\System\CurrentControlSet\Control\Remote Assistance", Name = "fAllowToGetHelp", Value = 0, Type = "dword" },
                // Disable NetBIOS over TCP/IP (prevent LLMNR/NBT-NS poisoning)
                new RegistryTweak { Path = @"HKLM\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces", Name = "NetbiosOptions", Value = 2, Type = "dword" }
            }
        };

        public SystemSettingsService(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

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

        public async Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object> settings)
        {
            return await Task.Run(() =>
            {
                var tweaks = new List<RegistryTweak>();
                if (settings == null) return tweaks;

                foreach (var userSetting in settings)
                {
                    string key = userSetting.Key.ToLower();
                    string val = userSetting.Value.ToString()?.ToLower() ?? "";

                    if (key == "security_preset")
                    {
                        if (_securityPresets.TryGetValue(val, out var presetTweaks))
                        {
                            tweaks.AddRange(presetTweaks);
                        }
                        else
                        {
                            Console.WriteLine($"[Warning] Unknown security preset '{val}'. Allowed: {string.Join(", ", _securityPresets.Keys)}");
                        }
                        continue;
                    }

                    if (_nonRegistryKeys.Contains(key)) continue;

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
            });
        }

        public Task ApplyNonRegistrySettingsAsync(Dictionary<string, object> settings, bool dryRun)
        {
            if (settings == null) return Task.CompletedTask;

            foreach (var userSetting in settings.Where(s => _nonRegistryKeys.Contains(s.Key.ToLower())))
            {
                string key = userSetting.Key.ToLower();
                switch (key)
                {
                    case "brightness":
                        if (int.TryParse(userSetting.Value.ToString(), out int brightness))
                        {
                            string command = $"(Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightnessMethods).WmiSetBrightness(1, {brightness})";
                            _processRunner.RunCommand("powershell", $"-Command \"{command}\"", dryRun);
                        }
                        break;
                    case "volume":
                        if (int.TryParse(userSetting.Value.ToString(), out int volume))
                        {
                            string command = $"Set-AudioDevice -PlaybackVolume {volume}";
                            _processRunner.RunCommand("powershell", $"-Command \"{command}\"", dryRun);
                        }
                        break;
                    case "notification":
                        if (userSetting.Value is Dictionary<object, object> notificationConfig)
                        {
                            var title = notificationConfig.GetValueOrDefault((object)"title")?.ToString() ?? "";
                            var message = notificationConfig.GetValueOrDefault((object)"message")?.ToString() ?? "";
                            string command = $"New-BurntToastNotification -Text '{title}', '{message}'";
                            _processRunner.RunCommand("powershell", $"-Command \"{command}\"", dryRun);
                        }
                        break;
                }
            }

            return Task.CompletedTask;
        }
    }
}