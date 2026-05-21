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
                    LogLevel.Warning,
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
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Brightness") && state.ToString()!.Contains(value.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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
                    LogLevel.Warning,
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
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("Volume") && state.ToString()!.Contains(value.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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
    }
}
