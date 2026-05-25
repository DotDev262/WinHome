using WinHome.Interfaces;
using WinHome.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace WinHome.Services.System
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IProcessRunner _processRunner;
        private readonly IRegistryService _registryService;
        private readonly ILogger<SystemSettingsService> _logger;
        private readonly List<string> _nonRegistryKeys = new() { "brightness", "volume", "notification", "screen_timeout_ac", "screen_timeout_dc", "sleep_timeout_ac", "sleep_timeout_dc" };

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
            },
            ["privacy"] = new()
            {
                // Disable Windows Telemetry data collection
                new RegistryTweak { Path = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", Name = "AllowTelemetry", Value = 0, Type = "dword" },
                // Disable Advertising ID for personalized ads
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", Name = "Enabled", Value = 0, Type = "dword" },
                // Disable Activity History feed
                new RegistryTweak { Path = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", Name = "EnableActivityFeed", Value = 0, Type = "dword" },
                // Disable Activity History cloud upload
                new RegistryTweak { Path = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", Name = "UploadUserActivities", Value = 0, Type = "dword" },
                // Disable Tailored Experiences based on diagnostic data
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy", Name = "TailoredExperiencesWithDiagnosticDataEnabled", Value = 0, Type = "dword" },
                // Disable Feedback Notifications
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\Siuf\Rules", Name = "NumberOfSIUFInPeriod", Value = 0, Type = "dword" },
                // Disable implicit text/ink collection for input personalization
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\InputPersonalization", Name = "RestrictImplicitTextCollection", Value = 1, Type = "dword" },
                // Disable contact harvesting for handwriting recognition
                new RegistryTweak { Path = @"HKCU\Software\Microsoft\InputPersonalization\TrainedDataStore", Name = "HarvestContacts", Value = 0, Type = "dword" }
            }
        };

        public SystemSettingsService(IProcessRunner processRunner, IRegistryService registryService, ILogger<SystemSettingsService> logger)
        {
            _processRunner = processRunner;
            _registryService = registryService;
            _logger = logger;
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

            new("desktop_icons_this_pc",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", "dword",
                new() { { "true", 0 }, { "false", 1 } }),

            new("desktop_icons_user_folder",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{59031A47-3F72-44A7-89C5-5595FE6B30EE}", "dword",
                new() { { "true", 0 }, { "false", 1 } }),

            new("desktop_icons_network",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", "dword",
                new() { { "true", 0 }, { "false", 1 } }),

            new("desktop_icons_recycle_bin",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{645FF040-5081-101B-9F08-00AA002F954E}", "dword",
                new() { { "true", 0 }, { "false", 1 } }),

            new("desktop_icons_control_panel",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", "dword",
                new() { { "true", 0 }, { "false", 1 } }),

            new("bing_search_enabled",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", "dword",
                new() { { "true", 1 }, { "false", 0 } }),

            new("taskbar_search",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", "dword",
                new() { { "hidden", 0 }, { "icon", 1 }, { "icon_label", 2 }, { "search_box", 3 } }),

            new("transparency",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", "dword",
                new() { { "true", 1 }, { "false", 0 } }),

            new("taskbar_autohide",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", "Settings", "binary",
                new()
                {
                    { "true", new byte[] { 0x30, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 } },
                    { "false", new byte[] { 0x30, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 } }
                }),

            new("taskbar_task_view",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", "dword",
                new() { { "true", 1 }, { "false", 0 } }),

            new("taskbar_end_task",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarEndTask", "dword",
                new() { { "true", 1 }, { "false", 0 } }),

            new("start_show_recent",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackDocs", "dword",
                new() { { "true", 1 }, { "false", 0 } }),

            new("snap_assist_flyout",
                @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", "dword",
                new() { { "true", 1 }, { "false", 0 } }),

            // clipboard_history
            new("clipboard_history",
                @"HKCU\Software\Microsoft\Clipboard", "EnableClipboardHistory", "dword",
                new() { { "true", 1 }, { "false", 0 } }),
        };

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
                        if (_securityPresets.TryGetValue(val, out var presetTweaks))
                        {
                            tweaks.AddRange(presetTweaks);
                        }
                        else
                        {
                            _logger.LogWarning("[Warning] Unknown security preset '{Preset}'. Allowed: {Allowed}", val, string.Join(", ", _securityPresets.Keys));
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
                            _logger.LogWarning("[Warning] Invalid value '{Value}' for setting '{Key}'. Allowed: {Allowed}", val, key, string.Join(", ", def.ValueMap.Keys));
                        }
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
                        if (regValue != null)
                        {
                            var match = def.ValueMap.FirstOrDefault(kvp =>
                            {
                                if (kvp.Value is byte[] kvpBytes && regValue is byte[] regBytes)
                                    return kvpBytes.SequenceEqual(regBytes);
                                return kvp.Value?.ToString() == regValue?.ToString();
                            });
                            if (!match.Equals(default(KeyValuePair<string, object>)))
                            {
                                object val = match.Key;
                                if (bool.TryParse(match.Key, out bool bVal)) val = bVal;
                                else if (int.TryParse(match.Key, out int iVal)) val = iVal;

                                captured[def.SettingKey] = val;
                            }
                        }
                    }
                    catch { /* Ignore read errors */ }
                }

                return captured;
            });
        }

        public string? GetFriendlyName(string registryPath, string registryName)
        {
            var match = _catalog.FirstOrDefault(d =>
                d.RegistryPath.Equals(registryPath, StringComparison.OrdinalIgnoreCase) &&
                d.RegistryName.Equals(registryName, StringComparison.OrdinalIgnoreCase));

            return match?.SettingKey;
        }

        public async Task<Dictionary<string, object>> CaptureOriginalSettingsAsync(Dictionary<string, object> settings)
        {
            return await Task.Run(() =>
            {
                var originals = new Dictionary<string, object>();
                if (settings == null) return originals;

                foreach (var setting in settings.Where(s => _nonRegistryKeys.Contains(s.Key.ToLower())))
                {
                    string key = setting.Key.ToLower();
                    try
                    {
                        switch (key)
                        {
                            case "brightness":
                                string brightCommand = "(Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightness).CurrentBrightness";
                                var brightResult = _processRunner.RunCommandWithOutput("powershell", new[] { "-Command", brightCommand });
                                if (int.TryParse(brightResult?.Trim(), out int currentBrightness))
                                    originals["brightness"] = currentBrightness;
                                break;

                            case "volume":
                                string volCommand = @"[Math]::Round((New-Object -ComObject MMDeviceEnumerator).GetDefaultAudioEndpoint(0,1).AudioEndpointVolume.MasterVolumeLevelScalar * 100)";
                                var volResult = _processRunner.RunCommandWithOutput("powershell", new[] { "-Command", volCommand });
                                if (int.TryParse(volResult?.Trim(), out int currentVolume))
                                    originals["volume"] = currentVolume;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Warning] Could not capture original value for '{key}': {ex.Message}");
                    }
                }

                return originals;
            });
        }

        public async Task RevertSystemSettingAsync(string settingKey, object originalValue, bool dryRun)
        {
            await Task.Run(() =>
            {
                try
                {
                    string key = settingKey.ToLower();
                    switch (key)
                    {
                        case "brightness":
                            if (int.TryParse(originalValue?.ToString(), out int brightness))
                            {
                                string command = $"(Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightnessMethods).WmiSetBrightness(1, {brightness})";
                                if (dryRun)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"[DryRun] Would revert brightness to {brightness}");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    _processRunner.RunCommand("powershell", new[] { "-Command", command }, false);
                                    Console.WriteLine($"[System Settings] Reverted brightness to {brightness}");
                                }
                            }
                            break;

                        case "volume":
                            if (int.TryParse(originalValue?.ToString(), out int volume))
                            {
                                string command = $@"$vol = [Math]::Round({volume} / 100.0, 2); (New-Object -ComObject MMDeviceEnumerator).GetDefaultAudioEndpoint(0,1).AudioEndpointVolume.MasterVolumeLevelScalar = $vol";
                                if (dryRun)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"[DryRun] Would revert volume to {volume}");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    _processRunner.RunCommand("powershell", new[] { "-Command", command }, false);
                                    Console.WriteLine($"[System Settings] Reverted volume to {volume}");
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to revert {settingKey}: {ex.Message}");
                }
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
                    case "brightness":
                        if (userSetting.Value == null) break;
                        if (!int.TryParse(userSetting.Value.ToString(), out int brightness))
                        {
                            _logger.LogWarning("Brightness value '{Value}' is not a valid integer.", userSetting.Value);
                            break;
                        }
                        if (brightness < 0 || brightness > 100)
                        {
                            _logger.LogWarning("Brightness value '{Value}' is out of range (0-100).", brightness);
                            break;
                        }
                        _processRunner.RunCommand("powershell", $"-Command \"(Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightnessMethods).WmiSetBrightness(1, {brightness})\"", dryRun);
                        break;

                    case "volume":
                        if (userSetting.Value == null) break;
                        if (!int.TryParse(userSetting.Value.ToString(), out int volume))
                        {
                            _logger.LogWarning("Volume value '{Value}' is not a valid integer.", userSetting.Value);
                            break;
                        }
                        if (volume < 0 || volume > 100)
                        {
                            _logger.LogWarning("Volume value '{Value}' is out of range (0-100).", volume);
                            break;
                        }
                        string volCommand = $@"$vol = [Math]::Round({volume} / 100.0, 2); (New-Object -ComObject MMDeviceEnumerator).GetDefaultAudioEndpoint(0,1).AudioEndpointVolume.MasterVolumeLevelScalar = $vol";
                        _processRunner.RunCommand("powershell", $"-Command \"{volCommand}\"", dryRun);
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
                    case "screen_timeout_ac":
                        ApplyPowerSetting("monitor-timeout-ac", userSetting.Value, dryRun);
                        break;
                    case "screen_timeout_dc":
                        ApplyPowerSetting("monitor-timeout-dc", userSetting.Value, dryRun);
                        break;
                    case "sleep_timeout_ac":
                        ApplyPowerSetting("standby-timeout-ac", userSetting.Value, dryRun);
                        break;
                    case "sleep_timeout_dc":
                        ApplyPowerSetting("standby-timeout-dc", userSetting.Value, dryRun);
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private void ApplyPowerSetting(string powercfgArg, object? value, bool dryRun)
        {
            if (value?.ToString() is string valStr && int.TryParse(valStr, out int minutes))
            {
                if (minutes < 0)
                {
                    _logger.LogWarning($"[Settings] Power setting value '{minutes}' cannot be negative. Skipping.");
                    return;
                }
                _processRunner.RunCommand("powercfg", $"/change {powercfgArg} {minutes}", dryRun);
            }
            else if (value != null)
            {
                _logger.LogWarning($"[Settings] Power setting value '{value}' is not a valid integer. Skipping.");
            }
        }
    }
}