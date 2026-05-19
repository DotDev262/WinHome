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
        private readonly Mock<ILogger> _mockLogger;
        private readonly SystemSettingsService _service;

        public SystemSettingsServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockRegistryService = new Mock<IRegistryService>();
            _mockLogger = new Mock<ILogger>();
            _service = new SystemSettingsService(_mockProcessRunner.Object, _mockRegistryService.Object, _mockLogger.Object);
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

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(50)]
        public async Task ApplyNonRegistrySettingsAsync_Should_Accept_Valid_Brightness_Boundaries(int brightness)
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", brightness }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains($"WmiSetBrightness(1, {brightness})")), false), Times.Once);
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task ApplyNonRegistrySettingsAsync_Should_LogWarning_For_Out_Of_Range_Brightness(int brightness)
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", brightness }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.IsAny<string>(), false), Times.Never);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("out of range"))), Times.Once);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("50.5")]
        public async Task ApplyNonRegistrySettingsAsync_Should_LogWarning_For_Invalid_Format_Brightness(string brightness)
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", brightness }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.IsAny<string>(), false), Times.Never);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Invalid brightness value"))), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(50)]
        public async Task ApplyNonRegistrySettingsAsync_Should_Accept_Valid_Volume_Boundaries(int volume)
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", volume }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.Is<string>(s => s.Contains($"Set-AudioDevice -PlaybackVolume {volume}")), false), Times.Once);
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task ApplyNonRegistrySettingsAsync_Should_LogWarning_For_Out_Of_Range_Volume(int volume)
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", volume }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.IsAny<string>(), false), Times.Never);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("out of range"))), Times.Once);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("50.5")]
        public async Task ApplyNonRegistrySettingsAsync_Should_LogWarning_For_Invalid_Format_Volume(string volume)
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", volume }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.IsAny<string>(), false), Times.Never);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Invalid volume value"))), Times.Once);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_LogWarning_For_Null_Brightness()
        {
            var settings = new Dictionary<string, object>
            {
                { "brightness", null! }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.IsAny<string>(), false), Times.Never);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Invalid brightness value 'null'"))), Times.Once);
        }

        [Fact]
        public async Task ApplyNonRegistrySettingsAsync_Should_LogWarning_For_Null_Volume()
        {
            var settings = new Dictionary<string, object>
            {
                { "volume", null! }
            };

            await _service.ApplyNonRegistrySettingsAsync(settings, false);

            _mockProcessRunner.Verify(r => r.RunCommand("powershell", It.IsAny<string>(), false), Times.Never);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Invalid volume value 'null'"))), Times.Once);
        }

        [Fact]
        public async Task GetTweaksAsync_Should_LogWarning_For_Null_Value()
        {
            var settings = new Dictionary<string, object>
            {
                { "taskbar_alignment", null! }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Empty(tweaksList);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Invalid value '' for setting 'taskbar_alignment'"))), Times.Once);
        }
    }
}
