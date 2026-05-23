using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class SystemSettingsServiceTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<IRegistryService> _mockRegistryService;
        private readonly Mock<ILogger<SystemSettingsService>> _mockLogger;
        private readonly SystemSettingsService _service;

        public SystemSettingsServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockRegistryService = new Mock<IRegistryService>();
            _mockLogger = new Mock<ILogger<SystemSettingsService>>();
            _service = new SystemSettingsService(
                _mockProcessRunner.Object,
                _mockRegistryService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_Set_Brightness()
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", 80 }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(
                r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("WmiSetBrightness(1, 80)")), false),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public async Task ApplyNonRegistrySettingsAsync_Brightness_BoundaryValues_Should_Apply(int value)
        {
            var settings = new Dictionary<string, object> { { "brightness", value } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(
                r => r.RunCommand("powershell", It.Is<string>(s => s.Contains($"WmiSetBrightness(1, {value})")), false),
                Times.Once);
            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(-100)]
        [InlineData(200)]
        public async Task ApplyNonRegistrySettingsAsync_Brightness_OutOfRange_Should_LogWarning_And_Skip(int value)
        {
            var settings = new Dictionary<string, object> { { "brightness", value } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Brightness") && state.ToString()!.Contains(value.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("50.5")]
        public async Task ApplyNonRegistrySettingsAsync_Brightness_InvalidFormat_Should_LogWarning_And_Skip(string value)
        {
            var settings = new Dictionary<string, object> { { "brightness", value } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Brightness") && state.ToString()!.Contains(value)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Brightness_Null_Should_SkipSilently()
        {
            var settings = new Dictionary<string, object> { { "brightness", null! } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_Set_Volume()
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", 50 }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(
                r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("Set-AudioDevice -PlaybackVolume 50")), false),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public async Task ApplyNonRegistrySettingsAsync_Volume_BoundaryValues_Should_Apply(int value)
        {
            var settings = new Dictionary<string, object> { { "volume", value } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(
                r => r.RunCommand("powershell", It.Is<string>(s => s.Contains($"Set-AudioDevice -PlaybackVolume {value}")), false),
                Times.Once);
            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(-100)]
        [InlineData(200)]
        public async Task ApplyNonRegistrySettingsAsync_Volume_OutOfRange_Should_LogWarning_And_Skip(int value)
        {
            var settings = new Dictionary<string, object> { { "volume", value } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Volume") && state.ToString()!.Contains(value.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("50.5")]
        public async Task ApplyNonRegistrySettingsAsync_Volume_InvalidFormat_Should_LogWarning_And_Skip(string value)
        {
            var settings = new Dictionary<string, object> { { "volume", value } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Volume") && state.ToString()!.Contains(value)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Volume_Null_Should_SkipSilently()
        {
            var settings = new Dictionary<string, object> { { "volume", null! } };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_Send_Notification()
        {
            var settings = new Dictionary<string, object>
            {
                { "notification", new Dictionary<object, object> { { "title", "Test Title" }, { "message", "Test Message" } } }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(
                r => r.RunCommand("powershell",
                    It.Is<string>(s => s.Contains("New-BurntToastNotification -Text 'Test Title', 'Test Message'")),
                    false),
                Times.Once);
        }

        [Theory]
        [InlineData("screen_timeout_ac", "monitor-timeout-ac")]
        [InlineData("screen_timeout_dc", "monitor-timeout-dc")]
        [InlineData("sleep_timeout_ac", "standby-timeout-ac")]
        [InlineData("sleep_timeout_dc", "standby-timeout-dc")]
        public async Task ApplyNonRegistrySettingsAsync_PowerSettings_ValidValues_Should_Apply(string key, string powercfgArg)
        {
            var settings = new Dictionary<string, object> { { key, 15 } };
            await _service.ApplyNonRegistrySettingsAsync(settings, false);
            _mockProcessRunner.Verify(
                r => r.RunCommand("powercfg", $"/change {powercfgArg} 15", false),
                Times.Once);
        }

        [Theory]
        [InlineData("screen_timeout_ac")]
        [InlineData("screen_timeout_dc")]
        [InlineData("sleep_timeout_ac")]
        [InlineData("sleep_timeout_dc")]
        public async Task ApplyNonRegistrySettingsAsync_PowerSettings_NegativeValue_Should_LogWarning_And_Skip(string key)
        {
            var settings = new Dictionary<string, object> { { key, -5 } };
            await _service.ApplyNonRegistrySettingsAsync(settings, false);
            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Power setting value") && state.ToString()!.Contains("-5")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Theory]
        [InlineData("screen_timeout_ac")]
        [InlineData("sleep_timeout_dc")]
        public async Task ApplyNonRegistrySettingsAsync_PowerSettings_InvalidFormat_Should_LogWarning_And_Skip(string key)
        {
            var settings = new Dictionary<string, object> { { key, "abc" } };
            await _service.ApplyNonRegistrySettingsAsync(settings, false);
            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Power setting value") && state.ToString()!.Contains("abc")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_PowerSettings_Null_Should_SkipSilently()
        {
            var settings = new Dictionary<string, object> { { "screen_timeout_ac", null! } };
            await _service.ApplyNonRegistrySettingsAsync(settings, false);
            _mockLogger.Verify(
                l => l.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
            _mockProcessRunner.Verify(
                r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Security_Presets()
        {
            var settings = new Dictionary<string, object>
            {
                { "security_preset", "baseline" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Contains(tweaksList, t => t.Name == "EnableMulticast" && t.Value.Equals(0));
            Assert.Contains(tweaksList, t => t.Name == "NoDriveTypeAutoRun" && t.Value.Equals(255));
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Strict_Security_Preset_Tweaks()
        {
            var settings = new Dictionary<string, object>
            {
                { "security_preset", "strict" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            // Baseline tweaks
            Assert.Contains(tweaksList, t => t.Name == "EnableWebContentEvaluation" && t.Value.Equals(1));
            Assert.Contains(tweaksList, t => t.Name == "NoDriveTypeAutoRun" && t.Value.Equals(255));
            Assert.Contains(tweaksList, t => t.Name == "EnableMulticast" && t.Value.Equals(0));

            // Strict tweaks
            Assert.Contains(tweaksList, t => t.Name == "Enabled" && t.Path == @"HKLM\Software\Microsoft\Windows Script Host\Settings" && t.Value.Equals(0));
            Assert.Contains(tweaksList, t => t.Name == "fAllowToGetHelp" && t.Path == @"HKLM\System\CurrentControlSet\Control\Remote Assistance" && t.Value.Equals(0));
            Assert.Contains(tweaksList, t => t.Name == "NetbiosOptions" && t.Path == @"HKLM\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces" && t.Value.Equals(2));
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Privacy_Preset_Tweaks()
        {
            var settings = new Dictionary<string, object>
            {
                { "security_preset", "privacy" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Equal(8, tweaksList.Count);
        }

        [Fact]
        public async Task GetTweaksAsync_Privacy_Preset_Should_Contain_Expected_Registry_Keys()
        {
            var settings = new Dictionary<string, object>
            {
                { "security_preset", "privacy" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Contains(tweaksList, t => t.Path == @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" && t.Name == "AllowTelemetry" && t.Value.Equals(0) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo" && t.Name == "Enabled" && t.Value.Equals(0) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System" && t.Name == "EnableActivityFeed" && t.Value.Equals(0) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System" && t.Name == "UploadUserActivities" && t.Value.Equals(0) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy" && t.Name == "TailoredExperiencesWithDiagnosticDataEnabled" && t.Value.Equals(0) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKCU\Software\Microsoft\Siuf\Rules" && t.Name == "NumberOfSIUFInPeriod" && t.Value.Equals(0) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKCU\Software\Microsoft\InputPersonalization" && t.Name == "RestrictImplicitTextCollection" && t.Value.Equals(1) && t.Type == "dword");
            Assert.Contains(tweaksList, t => t.Path == @"HKCU\Software\Microsoft\InputPersonalization\TrainedDataStore" && t.Name == "HarvestContacts" && t.Value.Equals(0) && t.Type == "dword");
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Empty_For_Unknown_Preset()
        {
            var settings = new Dictionary<string, object>
            {
                { "security_preset", "unknown_preset" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            Assert.NotNull(tweaks);
            Assert.Empty(tweaks);
        }

        [Theory]
        // dark_mode
        [InlineData("dark_mode", "true", "AppsUseLightTheme", 0)]
        [InlineData("dark_mode", "true", "SystemUsesLightTheme", 0)]
        [InlineData("dark_mode", "false", "AppsUseLightTheme", 1)]
        [InlineData("dark_mode", "false", "SystemUsesLightTheme", 1)]
        // taskbar_alignment
        [InlineData("taskbar_alignment", "left", "TaskbarAl", 0)]
        [InlineData("taskbar_alignment", "center", "TaskbarAl", 1)]
        // taskbar_widgets
        [InlineData("taskbar_widgets", "hide", "TaskbarDa", 0)]
        [InlineData("taskbar_widgets", "show", "TaskbarDa", 1)]
        // show_file_extensions
        [InlineData("show_file_extensions", "true", "HideFileExt", 0)]
        [InlineData("show_file_extensions", "false", "HideFileExt", 1)]
        // show_hidden_files
        [InlineData("show_hidden_files", "true", "Hidden", 1)]
        [InlineData("show_hidden_files", "false", "Hidden", 2)]
        // seconds_in_clock
        [InlineData("seconds_in_clock", "true", "ShowSecondsInSystemClock", 1)]
        [InlineData("seconds_in_clock", "false", "ShowSecondsInSystemClock", 0)]
        // explorer_launch_to
        [InlineData("explorer_launch_to", "this_pc", "LaunchTo", 1)]
        [InlineData("explorer_launch_to", "quick_access", "LaunchTo", 2)]
        // desktop_icons_this_pc
        [InlineData("desktop_icons_this_pc", "true", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0)]
        [InlineData("desktop_icons_this_pc", "false", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 1)]
        // desktop_icons_user_folder
        [InlineData("desktop_icons_user_folder", "true", "{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 0)]
        [InlineData("desktop_icons_user_folder", "false", "{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 1)]
        // desktop_icons_network
        [InlineData("desktop_icons_network", "true", "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 0)]
        [InlineData("desktop_icons_network", "false", "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 1)]
        // desktop_icons_recycle_bin
        [InlineData("desktop_icons_recycle_bin", "true", "{645FF040-5081-101B-9F08-00AA002F954E}", 0)]
        [InlineData("desktop_icons_recycle_bin", "false", "{645FF040-5081-101B-9F08-00AA002F954E}", 1)]
        // desktop_icons_control_panel
        [InlineData("desktop_icons_control_panel", "true", "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 0)]
        [InlineData("desktop_icons_control_panel", "false", "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 1)]
        // bing_search_enabled
        [InlineData("bing_search_enabled", "true", "BingSearchEnabled", 1)]
        [InlineData("bing_search_enabled", "false", "BingSearchEnabled", 0)]
        // taskbar_search
        [InlineData("taskbar_search", "hidden", "SearchboxTaskbarMode", 0)]
        [InlineData("taskbar_search", "icon", "SearchboxTaskbarMode", 1)]
        [InlineData("taskbar_search", "icon_label", "SearchboxTaskbarMode", 2)]
        [InlineData("taskbar_search", "search_box", "SearchboxTaskbarMode", 3)]
        // transparency
        [InlineData("transparency", "true", "EnableTransparency", 1)]
        [InlineData("transparency", "false", "EnableTransparency", 0)]
        // taskbar_task_view
        [InlineData("taskbar_task_view", "true", "ShowTaskViewButton", 1)]
        [InlineData("taskbar_task_view", "false", "ShowTaskViewButton", 0)]
        // taskbar_end_task
        [InlineData("taskbar_end_task", "true", "TaskbarEndTask", 1)]
        [InlineData("taskbar_end_task", "false", "TaskbarEndTask", 0)]
        // start_show_recent
        [InlineData("start_show_recent", "true", "Start_TrackDocs", 1)]
        [InlineData("start_show_recent", "false", "Start_TrackDocs", 0)]
        // snap_assist_flyout
        [InlineData("snap_assist_flyout", "true", "EnableSnapAssistFlyout", 1)]
        [InlineData("snap_assist_flyout", "false", "EnableSnapAssistFlyout", 0)]
        // clipboard_history
        [InlineData("clipboard_history", "true", "EnableClipboardHistory", 1)]
        [InlineData("clipboard_history", "false", "EnableClipboardHistory", 0)]
        public async Task GetTweaksAsync_Should_Return_Expected_Tweak(string key, string value, string expectedTweakName, object expectedValue)
        {
            var settings = new Dictionary<string, object> { { key, value } };
            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Contains(tweaksList, t => t.Name == expectedTweakName && t.Value.Equals(expectedValue));
        }

        [Theory]
        [InlineData("true", 0x03)]
        [InlineData("false", 0x02)]
        public async Task GetTweaksAsync_Should_Return_TaskbarAutoHide_Tweaks(string value, byte expectedByte)
        {
            var settings = new Dictionary<string, object> { { "taskbar_autohide", value } };
            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Single(tweaksList);
            Assert.Equal("Settings", tweaksList[0].Name);
            Assert.Equal("binary", tweaksList[0].Type);
            Assert.IsType<byte[]>(tweaksList[0].Value);

            var byteVal = (byte[])tweaksList[0].Value;
            Assert.Equal(expectedByte, byteVal[8]);
        }

        [Theory]
        // taskbar_alignment
        [InlineData("taskbar_alignment", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", 0, "left")]
        [InlineData("taskbar_alignment", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", 1, "center")]
        // taskbar_widgets
        [InlineData("taskbar_widgets", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0, "hide")]
        [InlineData("taskbar_widgets", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 1, "show")]
        // show_file_extensions
        [InlineData("show_file_extensions", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, true)]
        [InlineData("show_file_extensions", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 1, false)]
        // show_hidden_files
        [InlineData("show_hidden_files", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1, true)]
        [InlineData("show_hidden_files", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 2, false)]
        // seconds_in_clock
        [InlineData("seconds_in_clock", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", 1, true)]
        [InlineData("seconds_in_clock", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock", 0, false)]
        // explorer_launch_to
        [InlineData("explorer_launch_to", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, "this_pc")]
        [InlineData("explorer_launch_to", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 2, "quick_access")]
        // desktop_icons_this_pc
        [InlineData("desktop_icons_this_pc", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, true)]
        [InlineData("desktop_icons_this_pc", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 1, false)]
        // desktop_icons_user_folder
        [InlineData("desktop_icons_user_folder", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 0, true)]
        [InlineData("desktop_icons_user_folder", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 1, false)]
        // desktop_icons_network
        [InlineData("desktop_icons_network", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 0, true)]
        [InlineData("desktop_icons_network", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 1, false)]
        // desktop_icons_recycle_bin
        [InlineData("desktop_icons_recycle_bin", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{645FF040-5081-101B-9F08-00AA002F954E}", 0, true)]
        [InlineData("desktop_icons_recycle_bin", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{645FF040-5081-101B-9F08-00AA002F954E}", 1, false)]
        // desktop_icons_control_panel
        [InlineData("desktop_icons_control_panel", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 0, true)]
        [InlineData("desktop_icons_control_panel", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 1, false)]
        // bing_search_enabled
        [InlineData("bing_search_enabled", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1, true)]
        [InlineData("bing_search_enabled", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0, false)]
        // taskbar_search
        [InlineData("taskbar_search", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, "hidden")]
        [InlineData("taskbar_search", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1, "icon")]
        [InlineData("taskbar_search", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 2, "icon_label")]
        [InlineData("taskbar_search", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 3, "search_box")]

        // transparency
        [InlineData("transparency", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1, true)]
        [InlineData("transparency", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, false)]
        // taskbar_task_view
        [InlineData("taskbar_task_view", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 1, true)]
        [InlineData("taskbar_task_view", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0, false)]
        // taskbar_end_task
        [InlineData("taskbar_end_task", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarEndTask", 1, true)]
        [InlineData("taskbar_end_task", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarEndTask", 0, false)]
        // start_show_recent
        [InlineData("start_show_recent", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackDocs", 1, true)]
        [InlineData("start_show_recent", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackDocs", 0, false)]
        // snap_assist_flyout
        [InlineData("snap_assist_flyout", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", 1, true)]
        [InlineData("snap_assist_flyout", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", 0, false)]
        // clipboard_history
        [InlineData("clipboard_history", @"HKCU\Software\Microsoft\Clipboard", "EnableClipboardHistory", 1, true)]
        [InlineData("clipboard_history", @"HKCU\Software\Microsoft\Clipboard", "EnableClipboardHistory", 0, false)]
        public async Task GetCapturedSettingsAsync_Should_Capture_Setting(string key, string path, string name, object registryValue, object expectedCapturedValue)
        {
            _mockRegistryService.Setup(r => r.Read(path, name)).Returns(registryValue);

            var captured = await _service.GetCapturedSettingsAsync();

            Assert.True(captured.ContainsKey(key));
            Assert.Equal(expectedCapturedValue, captured[key]);
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(1, 1, false)]
        public async Task GetCapturedSettingsAsync_Should_Capture_DarkMode(int appsValue, int systemValue, bool expected)
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme"))
                .Returns(appsValue);
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme"))
                .Returns(systemValue);

            var captured = await _service.GetCapturedSettingsAsync();

            Assert.True(captured.ContainsKey("dark_mode"));
            Assert.Equal(expected, captured["dark_mode"]);
        }

        [Theory]
        [InlineData(0x03, true)]
        [InlineData(0x02, false)]
        public async Task GetCapturedSettingsAsync_Should_Capture_TaskbarAutoHide(byte settingByte, bool expected)
        {
            var mockBytes = new byte[] { 0x30, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, settingByte, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", "Settings"))
                .Returns(mockBytes);

            var captured = await _service.GetCapturedSettingsAsync();

            Assert.True(captured.ContainsKey("taskbar_autohide"));
            Assert.Equal(expected, captured["taskbar_autohide"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Empty_On_Null_Settings()
        {
            var tweaks = await _service.GetTweaksAsync(null);
            Assert.NotNull(tweaks);
            Assert.Empty(tweaks);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Empty_On_Empty_Settings()
        {
            var tweaks = await _service.GetTweaksAsync(new Dictionary<string, object>());
            Assert.NotNull(tweaks);
            Assert.Empty(tweaks);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Ignore_Unknown_Settings()
        {
            var settings = new Dictionary<string, object>
            {
                { "some_unknown_setting_xyz", "true" }
            };
            var tweaks = await _service.GetTweaksAsync(settings);
            Assert.NotNull(tweaks);
            Assert.Empty(tweaks);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Ignore_Invalid_Values()
        {
            var settings = new Dictionary<string, object>
            {
                { "taskbar_alignment", "invalid_value" }
            };
            var tweaks = await _service.GetTweaksAsync(settings);
            Assert.NotNull(tweaks);
            Assert.Empty(tweaks);
        }

        [Fact]
        public void GetFriendlyName_Should_Return_Correct_Key_For_Known_Registry_Tweak()
        {
            var key1 = _service.GetFriendlyName(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme");
            Assert.Equal("dark_mode", key1);

            var key2 = _service.GetFriendlyName(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl");
            Assert.Equal("taskbar_alignment", key2);

            var key3 = _service.GetFriendlyName(@"HKCU\Software\Microsoft\Clipboard", "EnableClipboardHistory");
            Assert.Equal("clipboard_history", key3);

            var key4 = _service.GetFriendlyName(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
            Assert.Equal("desktop_icons_this_pc", key4);
        }

        [Fact]
        public void GetFriendlyName_Should_Return_Null_For_Unknown_Registry_Tweak()
        {
            var key = _service.GetFriendlyName(@"HKCU\Unknown\Path", "UnknownName");
            Assert.Null(key);
        }
    }
}