using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Orchestrates system-level settings configuration, mapping abstract user preferences 
    /// to concrete Windows Registry modifications or PowerShell-driven system commands.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IProcessRunner _processRunner;
        private readonly IRegistryService _registryService;
        private readonly ILogger<SystemSettingsService> _logger;
        
        private readonly List<string> _nonRegistryKeys = new() { "brightness", "volume", "notification" };

        private const int MinVolumeOrBrightness = 0;
        private const int MaxVolumeOrBrightness = 100;

        private readonly Dictionary<string, List<RegistryTweak>> _securityPresets = new()
        {
            ["baseline"] = new()
            {
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost", Name = "EnableWebContentEvaluation", Value = 1, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", Name = "NoDriveTypeAutoRun", Value = 255, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\Software\Policies\Microsoft\Windows NT\DNSClient", Name = "EnableMulticast", Value = 0, Type = "dword" }
            },
            ["strict"] = new()
            {
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost", Name = "EnableWebContentEvaluation", Value = 1, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", Name = "NoDriveTypeAutoRun", Value = 255, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\Software\Policies\Microsoft\Windows NT\DNSClient", Name = "EnableMulticast", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\Software\Microsoft\Windows Script Host\Settings", Name = "Enabled", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\System\CurrentControlSet\Control\Remote Assistance", Name = "fAllowToGetHelp", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces", Name = "NetbiosOptions", Value = 2, Type = "dword" }
            },
            ["privacy"] = new()
            {
                new RegistryTweak { Path = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", Name = "AllowTelemetry", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", Name = "Enabled", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", Name = "EnableActivityFeed", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", Name = "UploadUserActivities", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy", Name = "TailoredExperiencesWithDiagnosticDataEnabled", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Siuf\Rules", Name = "NumberOfSIUFInPeriod", Value = 0, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\InputPersonalization", Name = "RestrictImplicitTextCollection", Value = 1, Type = "dword" },
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\InputPersonalization\TrainedDataStore", Name = "HarvestContacts", Value = 0, Type = "dword" }
            }
        };

        private readonly List<SettingDefinition> _catalog = new()
        {
            new("dark_mode", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "dword", new() { { "true", 0 }, { "false", 1 } }),
            new("dark_mode", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", "dword", new() { { "true", 0 }, { "false", 1 } }),
            new("taskbar_alignment", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", "dword", new() { { "left", 0 }, { "center", 1 } }),
            new("taskbar_widgets", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", "dword", new() { { "hide", 0 }, { "show", 1 } }),
            new("show_file_extensions", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", "dword", new() { { "true", 0 }, { "false", 1 } }),
            new("show_hidden_files", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", "dword", new() { { "true", 1 }, { "false", 2 } }),
            new("seconds_in_clock", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", "dword", new() { { "true", 1 }, { "false", 0 } }),
            new("explorer_launch_to", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", "dword", new() { { "this_pc", 1 }, { "quick_access", 2 } }),
            new("bing_search_enabled", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", "dword", new() { { "true", 1 }, { "false", 0 } }),
            new("transparency", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", "dword", new() { { "true", 1 }, { "false", 0 } }),
            new("taskbar_task_view", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", "dword", new() { { "true", 1 }, { "false", 0 } }),
            new("taskbar_end_task", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarEndTask", "dword", new() { { "true", 1 }, { "false", 0 } }),
            new("start_show_recent", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackDocs", "dword", new() { { "true", 1 }, { "false", 0 } }),
            new("snap_assist_flyout", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", "dword", new() { { "true", 1 }, { "false", 0 } })
        };

        private record SettingDefinition(string SettingKey, string RegistryPath, string RegistryName, string RegistryType, Dictionary<string, object> ValueMap);

        public SystemSettingsService(IProcessRunner processRunner, IRegistryService registryService, ILogger<SystemSettingsService> logger)
        {
            _processRunner = processRunner;
            _registryService = registryService;
            _logger = logger;
        }

        public async Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object>? settings)
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
                        if (_securityPresets.TryGetValue(val, out var presetTweaks)) tweaks.AddRange(presetTweaks);
                        else _logger.LogWarning($"[Settings] Unknown security preset '{val}'.");
                        continue;
                    }

                    if (_nonRegistryKeys.Contains(key)) continue;

                    foreach (var def in _catalog.Where(d => d.SettingKey == key))
                    {
                        if (def.ValueMap.TryGetValue(val, out object? regValue))
                        {
                            tweaks.Add(new RegistryTweak { Path = def.RegistryPath, Name = def.RegistryName, Value = regValue, Type = def.RegistryType });
                        }
                        else _logger.LogWarning($"[Settings] Invalid value '{val}' for '{key}'.");
                    }
                }
                return tweaks;
            });
        }

        public async Task<Dictionary<string, object>> GetCapturedSettingsAsync()
        {
            return await Task.Run(() =>
            {
                var captured = new Dictionary<string, object>();
                foreach (var def in _catalog)
                {
                    try
                    {
                        var regValue = _registryService.Read(def.RegistryPath, def.RegistryName);
                        if (regValue == null) continue;

                        var match = def.ValueMap.FirstOrDefault(kvp =>
                        {
                            if (kvp.Value is byte[] kvpBytes && regValue is byte[] regBytes)
                                return kvpBytes.SequenceEqual(regBytes);
                            return kvp.Value?.ToString() == regValue.ToString();
                        });

                        if (!match.Equals(default(KeyValuePair<string, object>)))
                        {
                            object val = match.Key;
                            if (bool.TryParse(match.Key, out bool bVal)) val = bVal;
                            else if (int.TryParse(match.Key, out int iVal)) val = iVal;
                            captured[def.SettingKey] = val;
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning($"[Settings] Failed to read {def.RegistryName}: {ex.Message}"); }
                }
                return captured;
            });
        }

        public Task ApplyNonRegistrySettingsAsync(Dictionary<string, object>? settings, bool dryRun)
        {
            if (settings == null) return Task.CompletedTask;

            foreach (var userSetting in settings.Where(s => _nonRegistryKeys.Contains(s.Key.ToLower())))
            {
                string key = userSetting.Key.ToLower();
                switch (key)
                {
                    case "brightness" when int.TryParse(userSetting.Value?.ToString(), out int b) && b is >= 0 and <= 100:
                        _processRunner.RunCommand("powershell", $"-Command \"(Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightnessMethods).WmiSetBrightness(1, {b})\"", dryRun);
                        break;
                    case "volume" when int.TryParse(userSetting.Value?.ToString(), out int v) && v is >= 0 and <= 100:
                        _processRunner.RunCommand("powershell", $"-Command \"Set-AudioDevice -PlaybackVolume {v}\"", dryRun);
                        break;
                    case "notification" when userSetting.Value is Dictionary<object, object> cfg:
                        _processRunner.RunCommand("powershell", $"-Command \"New-BurntToastNotification -Text '{cfg.GetValueOrDefault((object)"title")}', '{cfg.GetValueOrDefault((object)"message")}'\"", dryRun);
                        break;
                }
            }
            return Task.CompletedTask;
        }

        public string? GetFriendlyName(string registryPath, string registryName) =>
            _catalog.FirstOrDefault(d => d.RegistryPath.Equals(registryPath, StringComparison.OrdinalIgnoreCase) && d.RegistryName.Equals(registryName, StringComparison.OrdinalIgnoreCase))?.SettingKey;
    }
}