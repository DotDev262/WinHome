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

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("MasterVolumeLevelScalar")), false), Times.Once);
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
        public async Task CaptureOriginalSettingsAsync_Should_Capture_Brightness()
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", 80 }
            };

            _mockProcessRunner
                .Setup(r => r.RunCommandWithOutput("powershell", It.Is<string>(s => s.Contains("CurrentBrightness"))))
                .Returns("50");

            var originals = await _service.CaptureOriginalSettingsAsync(settings);

            Assert.True(originals.ContainsKey("brightness"));
            Assert.Equal(50, originals["brightness"]);
        }

        [Fact]
        public async Task CaptureOriginalSettingsAsync_Should_Capture_Volume()
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", 70 }
            };

            _mockProcessRunner
                .Setup(r => r.RunCommandWithOutput("powershell", It.Is<string>(s => s.Contains("MasterVolumeLevelScalar"))))
                .Returns("60");

            var originals = await _service.CaptureOriginalSettingsAsync(settings);

            Assert.True(originals.ContainsKey("volume"));
            Assert.Equal(60, originals["volume"]);
        }

        [Fact]
        public async Task RevertSystemSettingAsync_Should_Revert_Brightness()
        {
            const int originalBrightness = 50;

            await _service.RevertSystemSettingAsync("brightness", originalBrightness, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("WmiSetBrightness(1, 50)")), false), Times.Once);
        }

        [Fact]
        public async Task RevertSystemSettingAsync_Should_Revert_Volume()
        {
            const int originalVolume = 60;

            await _service.RevertSystemSettingAsync("volume", originalVolume, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains("MasterVolumeLevelScalar")), false), Times.Once);
        }

        [Fact]
        public async Task RevertSystemSettingAsync_DryRun_Should_Not_Execute_Command()
        {
            const int originalBrightness = 50;

            await _service.RevertSystemSettingAsync("brightness", originalBrightness, true);

            _mockProcessRunner.Verify(r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task CaptureOriginalSettingsAsync_Should_Skip_Notifications()
        {
            var settings = new Dictionary<string, object>
            {
                { "notification", new Dictionary<object, object> { { "title", "Test" }, { "message", "Message" } } }
            };

            var originals = await _service.CaptureOriginalSettingsAsync(settings);

            Assert.Empty(originals);
        }
        
        [Fact]
        public async Task RevertSystemSettingAsync_DryRun_Should_Not_Execute_Command_Volume()
        {
            const int originalVolume = 60;

            await _service.RevertSystemSettingAsync("volume", originalVolume, true);

            _mockProcessRunner.Verify(r => r.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task CaptureOriginalSettingsAsync_Should_Handle_Invalid_Output()
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", 80 }
            };

            _mockProcessRunner
                .Setup(r => r.RunCommandWithOutput("powershell", It.Is<string>(s => s.Contains("CurrentBrightness"))))
                .Returns((string)null);

            var originals = await _service.CaptureOriginalSettingsAsync(settings);

            Assert.Empty(originals);
        }
    }
}
