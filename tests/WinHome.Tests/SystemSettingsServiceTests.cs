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

            // Telemetry data collection
            Assert.Contains(tweaksList, t => t.Name == "AllowTelemetry" && t.Value.Equals(0));
            // Advertising ID
            Assert.Contains(tweaksList, t => t.Name == "Enabled" && t.Path.Contains("AdvertisingInfo") && t.Value.Equals(0));
            // Activity History
            Assert.Contains(tweaksList, t => t.Name == "EnableActivityFeed" && t.Value.Equals(0));
            Assert.Contains(tweaksList, t => t.Name == "UploadUserActivities" && t.Value.Equals(0));
            // Tailored Experiences
            Assert.Contains(tweaksList, t => t.Name == "TailoredExperiencesWithDiagnosticDataEnabled" && t.Value.Equals(0));
            // Feedback Notifications
            Assert.Contains(tweaksList, t => t.Name == "NumberOfSIUFInPeriod" && t.Value.Equals(0));
            // Input Personalization
            Assert.Contains(tweaksList, t => t.Name == "RestrictImplicitTextCollection" && t.Value.Equals(1));
            Assert.Contains(tweaksList, t => t.Name == "HarvestContacts" && t.Value.Equals(0));
        }

        [Fact]
        public async Task GetTweaksAsync_Should_Return_Empty_For_Unknown_Preset()
        {
            var settings = new Dictionary<string, object>
            {
                { "security_preset", "nonexistent" }
            };

            var tweaks = await _service.GetTweaksAsync(settings);
            var tweaksList = new List<RegistryTweak>(tweaks);

            Assert.Empty(tweaksList);
        }
    }
}
