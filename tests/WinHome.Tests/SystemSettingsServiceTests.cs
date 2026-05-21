using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly SystemSettingsService _service;

        public SystemSettingsServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockRegistryService = new Mock<IRegistryService>();
            _service = new SystemSettingsService(_mockProcessRunner.Object, _mockRegistryService.Object);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_Set_Brightness()
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", 80 }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("WmiSetBrightness(1, 80)")), false), Times.Once);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_Set_Volume()
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", 50 }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("Set-AudioDevice -PlaybackVolume 50")), false), Times.Once);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_Send_Notification()
        {
            var settings = new Dictionary<string, object>
            {
                { "notification", new Dictionary<object, object> { { "title", "Test Title" }, { "message", "Test Message" } } }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("New-BurntToastNotification -Text 'Test Title', 'Test Message'")), false), Times.Once);
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
        public async Task GetTweaksAsync_Should_Return_DarkMode_Tweaks()
        {
            var settingsTrue = new Dictionary<string, object> { { "dark_mode", "true" } };
            var tweaksTrue = await _service.GetTweaksAsync(settingsTrue);
            var tweaksListTrue = new List<RegistryTweak>(tweaksTrue);

            Assert.Equal(2, tweaksListTrue.Count);
            Assert.Contains(tweaksListTrue, t => t.Name == "AppsUseLightTheme" && t.Value.Equals(0));
            Assert.Contains(tweaksListTrue, t => t.Name == "SystemUsesLightTheme" && t.Value.Equals(0));

            var settingsFalse = new Dictionary<string, object> { { "dark_mode", "false" } };
            var tweaksFalse = await _service.GetTweaksAsync(settingsFalse);
            var tweaksListFalse = new List<RegistryTweak>(tweaksFalse);

            Assert.Equal(2, tweaksListFalse.Count);
            Assert.Contains(tweaksListFalse, t => t.Name == "AppsUseLightTheme" && t.Value.Equals(1));
            Assert.Contains(tweaksListFalse, t => t.Name == "SystemUsesLightTheme" && t.Value.Equals(1));
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_DarkMode()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme"))
                .Returns(0);
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme"))
                .Returns(0);

            var capturedTrue = await _service.GetCapturedSettingsAsync();
            Assert.True(capturedTrue.ContainsKey("dark_mode"));
            Assert.True((bool)capturedTrue["dark_mode"]);

            _mockRegistryService.Invocations.Clear();
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme"))
                .Returns(1);
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme"))
                .Returns(1);

            var capturedFalse = await _service.GetCapturedSettingsAsync();
            Assert.True(capturedFalse.ContainsKey("dark_mode"));
            Assert.False((bool)capturedFalse["dark_mode"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_TaskbarAlignment_Tweaks()
        {
            var settingsLeft = new Dictionary<string, object> { { "taskbar_alignment", "left" } };
            var tweaksLeft = await _service.GetTweaksAsync(settingsLeft);
            var listLeft = new List<RegistryTweak>(tweaksLeft);
            Assert.Single(listLeft);
            Assert.Equal("TaskbarAl", listLeft[0].Name);
            Assert.Equal(0, listLeft[0].Value);

            var settingsCenter = new Dictionary<string, object> { { "taskbar_alignment", "center" } };
            var tweaksCenter = await _service.GetTweaksAsync(settingsCenter);
            var listCenter = new List<RegistryTweak>(tweaksCenter);
            Assert.Single(listCenter);
            Assert.Equal("TaskbarAl", listCenter[0].Name);
            Assert.Equal(1, listCenter[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_TaskbarAlignment()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl"))
                .Returns(0);
            var capLeft = await _service.GetCapturedSettingsAsync();
            Assert.Equal("left", capLeft["taskbar_alignment"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl"))
                .Returns(1);
            var capCenter = await _service.GetCapturedSettingsAsync();
            Assert.Equal("center", capCenter["taskbar_alignment"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_TaskbarWidgets_Tweaks()
        {
            var settingsHide = new Dictionary<string, object> { { "taskbar_widgets", "hide" } };
            var tweaksHide = await _service.GetTweaksAsync(settingsHide);
            var listHide = new List<RegistryTweak>(tweaksHide);
            Assert.Single(listHide);
            Assert.Equal("TaskbarDa", listHide[0].Name);
            Assert.Equal(0, listHide[0].Value);

            var settingsShow = new Dictionary<string, object> { { "taskbar_widgets", "show" } };
            var tweaksShow = await _service.GetTweaksAsync(settingsShow);
            var listShow = new List<RegistryTweak>(tweaksShow);
            Assert.Single(listShow);
            Assert.Equal("TaskbarDa", listShow[0].Name);
            Assert.Equal(1, listShow[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_TaskbarWidgets()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa"))
                .Returns(0);
            var capHide = await _service.GetCapturedSettingsAsync();
            Assert.Equal("hide", capHide["taskbar_widgets"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa"))
                .Returns(1);
            var capShow = await _service.GetCapturedSettingsAsync();
            Assert.Equal("show", capShow["taskbar_widgets"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_ShowFileExtensions_Tweaks()
        {
            var settingsTrue = new Dictionary<string, object> { { "show_file_extensions", "true" } };
            var tweaksTrue = await _service.GetTweaksAsync(settingsTrue);
            var listTrue = new List<RegistryTweak>(tweaksTrue);
            Assert.Single(listTrue);
            Assert.Equal("HideFileExt", listTrue[0].Name);
            Assert.Equal(0, listTrue[0].Value);

            var settingsFalse = new Dictionary<string, object> { { "show_file_extensions", "false" } };
            var tweaksFalse = await _service.GetTweaksAsync(settingsFalse);
            var listFalse = new List<RegistryTweak>(tweaksFalse);
            Assert.Single(listFalse);
            Assert.Equal("HideFileExt", listFalse[0].Name);
            Assert.Equal(1, listFalse[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_ShowFileExtensions()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt"))
                .Returns(0);
            var capTrue = await _service.GetCapturedSettingsAsync();
            Assert.True((bool)capTrue["show_file_extensions"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt"))
                .Returns(1);
            var capFalse = await _service.GetCapturedSettingsAsync();
            Assert.False((bool)capFalse["show_file_extensions"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_ShowHiddenFiles_Tweaks()
        {
            var settingsTrue = new Dictionary<string, object> { { "show_hidden_files", "true" } };
            var tweaksTrue = await _service.GetTweaksAsync(settingsTrue);
            var listTrue = new List<RegistryTweak>(tweaksTrue);
            Assert.Single(listTrue);
            Assert.Equal("Hidden", listTrue[0].Name);
            Assert.Equal(1, listTrue[0].Value);

            var settingsFalse = new Dictionary<string, object> { { "show_hidden_files", "false" } };
            var tweaksFalse = await _service.GetTweaksAsync(settingsFalse);
            var listFalse = new List<RegistryTweak>(tweaksFalse);
            Assert.Single(listFalse);
            Assert.Equal("Hidden", listFalse[0].Name);
            Assert.Equal(2, listFalse[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_ShowHiddenFiles()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden"))
                .Returns(1);
            var capTrue = await _service.GetCapturedSettingsAsync();
            Assert.True((bool)capTrue["show_hidden_files"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden"))
                .Returns(2);
            var capFalse = await _service.GetCapturedSettingsAsync();
            Assert.False((bool)capFalse["show_hidden_files"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_SecondsInClock_Tweaks()
        {
            var settingsTrue = new Dictionary<string, object> { { "seconds_in_clock", "true" } };
            var tweaksTrue = await _service.GetTweaksAsync(settingsTrue);
            var listTrue = new List<RegistryTweak>(tweaksTrue);
            Assert.Single(listTrue);
            Assert.Equal("ShowSecondsInSystemClock", listTrue[0].Name);
            Assert.Equal(1, listTrue[0].Value);

            var settingsFalse = new Dictionary<string, object> { { "seconds_in_clock", "false" } };
            var tweaksFalse = await _service.GetTweaksAsync(settingsFalse);
            var listFalse = new List<RegistryTweak>(tweaksFalse);
            Assert.Single(listFalse);
            Assert.Equal("ShowSecondsInSystemClock", listFalse[0].Name);
            Assert.Equal(0, listFalse[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_SecondsInClock()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock"))
                .Returns(1);
            var capTrue = await _service.GetCapturedSettingsAsync();
            Assert.True((bool)capTrue["seconds_in_clock"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSecondsInSystemClock"))
                .Returns(0);
            var capFalse = await _service.GetCapturedSettingsAsync();
            Assert.False((bool)capFalse["seconds_in_clock"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_ExplorerLaunchTo_Tweaks()
        {
            var settingsThisPc = new Dictionary<string, object> { { "explorer_launch_to", "this_pc" } };
            var tweaksThisPc = await _service.GetTweaksAsync(settingsThisPc);
            var listThisPc = new List<RegistryTweak>(tweaksThisPc);
            Assert.Single(listThisPc);
            Assert.Equal("LaunchTo", listThisPc[0].Name);
            Assert.Equal(1, listThisPc[0].Value);

            var settingsQuick = new Dictionary<string, object> { { "explorer_launch_to", "quick_access" } };
            var tweaksQuick = await _service.GetTweaksAsync(settingsQuick);
            var listQuick = new List<RegistryTweak>(tweaksQuick);
            Assert.Single(listQuick);
            Assert.Equal("LaunchTo", listQuick[0].Name);
            Assert.Equal(2, listQuick[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_ExplorerLaunchTo()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo"))
                .Returns(1);
            var capThisPc = await _service.GetCapturedSettingsAsync();
            Assert.Equal("this_pc", capThisPc["explorer_launch_to"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo"))
                .Returns(2);
            var capQuick = await _service.GetCapturedSettingsAsync();
            Assert.Equal("quick_access", capQuick["explorer_launch_to"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_BingSearchEnabled_Tweaks()
        {
            var settingsTrue = new Dictionary<string, object> { { "bing_search_enabled", "true" } };
            var tweaksTrue = await _service.GetTweaksAsync(settingsTrue);
            var listTrue = new List<RegistryTweak>(tweaksTrue);
            Assert.Single(listTrue);
            Assert.Equal("BingSearchEnabled", listTrue[0].Name);
            Assert.Equal(1, listTrue[0].Value);

            var settingsFalse = new Dictionary<string, object> { { "bing_search_enabled", "false" } };
            var tweaksFalse = await _service.GetTweaksAsync(settingsFalse);
            var listFalse = new List<RegistryTweak>(tweaksFalse);
            Assert.Single(listFalse);
            Assert.Equal("BingSearchEnabled", listFalse[0].Name);
            Assert.Equal(0, listFalse[0].Value);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_BingSearchEnabled()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled"))
                .Returns(1);
            var capTrue = await _service.GetCapturedSettingsAsync();
            Assert.True((bool)capTrue["bing_search_enabled"]);

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled"))
                .Returns(0);
            var capFalse = await _service.GetCapturedSettingsAsync();
            Assert.False((bool)capFalse["bing_search_enabled"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Transparency_Tweaks()
        {
            var settings = new Dictionary<string, object>
            {
                { "transparency", "true" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Single(tweaksList);
            Assert.Equal("EnableTransparency", tweaksList[0].Name);
            Assert.Equal(1, tweaksList[0].Value);
            Assert.Equal("dword", tweaksList[0].Type);
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_Transparency()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency"))
                .Returns(1);

            var captured = await _service.GetCapturedSettingsAsync();

            Assert.True(captured.ContainsKey("transparency"));
            Assert.Equal(true, captured["transparency"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_TaskbarAutoHide_Tweaks()
        {
            var settings = new Dictionary<string, object>
            {
                { "taskbar_autohide", "true" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Single(tweaksList);
            Assert.Equal("Settings", tweaksList[0].Name);
            Assert.Equal("binary", tweaksList[0].Type);
            Assert.IsType<byte[]>(tweaksList[0].Value);

            var byteVal = (byte[])tweaksList[0].Value;
            Assert.Equal(0x03, byteVal[8]); // 9th byte is 0x03 for auto-hide enable
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_TaskbarAutoHide()
        {
            var mockBytes = new byte[] { 0x30, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", "Settings"))
                .Returns(mockBytes);

            var captured = await _service.GetCapturedSettingsAsync();

            Assert.True(captured.ContainsKey("taskbar_autohide"));
            Assert.Equal(true, captured["taskbar_autohide"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Remaining_Custom_Tweaks()
        {
            var settings = new Dictionary<string, object>
            {
                { "taskbar_task_view", "true" },
                { "taskbar_end_task", "true" },
                { "start_show_recent", "true" },
                { "snap_assist_flyout", "true" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Equal(4, tweaksList.Count);

            Assert.Contains(tweaksList, t => t.Name == "ShowTaskViewButton" && t.Value.Equals(1));
            Assert.Contains(tweaksList, t => t.Name == "TaskbarEndTask" && t.Value.Equals(1));
            Assert.Contains(tweaksList, t => t.Name == "Start_TrackDocs" && t.Value.Equals(1));
            Assert.Contains(tweaksList, t => t.Name == "EnableSnapAssistFlyout" && t.Value.Equals(1));
        }

        [Fact]
        public async Task GetCapturedSettingsAsync_Should_Capture_Remaining_Custom_Settings()
        {
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton"))
                .Returns(1);
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarEndTask"))
                .Returns(1);
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackDocs"))
                .Returns(1);
            _mockRegistryService
                .Setup(r => r.Read(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout"))
                .Returns(1);

            var captured = await _service.GetCapturedSettingsAsync();

            Assert.True((bool)captured["taskbar_task_view"]);
            Assert.True((bool)captured["taskbar_end_task"]);
            Assert.True((bool)captured["start_show_recent"]);
            Assert.True((bool)captured["snap_assist_flyout"]);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Empty_On_Null_Settings()
        {
            var tweaks = await _service.GetTweaksAsync(null!);
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
        }

        [Fact]
        public void GetFriendlyName_Should_Return_Null_For_Unknown_Registry_Tweak()
        {
            var key = _service.GetFriendlyName(@"HKCU\Unknown\Path", "UnknownName");
            Assert.Null(key);
        }
    }
}

