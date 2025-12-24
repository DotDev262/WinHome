using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using Xunit;

namespace WinHome.Tests
{
    public class EngineTests
    {
        private readonly Mock<IPackageManager> _mockWinget;
        private readonly Mock<IDotfileService> _mockDotfiles;
        private readonly Mock<IRegistryService> _mockRegistry;
        private readonly Mock<ISystemSettingsService> _mockSystemSettings;
        private readonly Mock<IWslService> _mockWsl;
        private readonly Mock<IGitService> _mockGit;
        private readonly Mock<IEnvironmentService> _mockEnv;
        private readonly Dictionary<string, IPackageManager> _managers;

        public EngineTests()
        {
            // 1. Create Mocks
            _mockWinget = new Mock<IPackageManager>();
            _mockDotfiles = new Mock<IDotfileService>();
            _mockRegistry = new Mock<IRegistryService>();
            _mockSystemSettings = new Mock<ISystemSettingsService>();
            _mockWsl = new Mock<IWslService>();
            _mockGit = new Mock<IGitService>();
            _mockEnv = new Mock<IEnvironmentService>();

            // Setup basic behavior
            _mockWinget.Setup(x => x.IsAvailable()).Returns(true);
            _mockSystemSettings.Setup(x => x.GetTweaksAsync(It.IsAny<Dictionary<string, object>>()))
                               .Returns(Task.FromResult<IEnumerable<RegistryTweak>>(new List<RegistryTweak>()));

            // 2. Setup Manager Dictionary
            _managers = new Dictionary<string, IPackageManager>
            {
                { "winget", _mockWinget.Object }
            };
        }

        [Fact]
        public async Task RunAsync_ShouldInstallApps_WhenConfigured()
        {
            // Arrange
            var config = new Configuration();
            config.Apps.Add(new AppConfig { Id = "TestApp", Manager = "winget" });

            var engine = new Engine(
                _managers, 
                _mockDotfiles.Object, 
                _mockRegistry.Object, 
                _mockSystemSettings.Object, 
                _mockWsl.Object, 
                _mockGit.Object, 
                _mockEnv.Object
            );

            // Act
            // dryRun = false
            await engine.RunAsync(config, false);

            // Assert
            // Verify that Install was called exactly once for "TestApp"
            _mockWinget.Verify(x => x.Install(
                It.Is<AppConfig>(a => a.Id == "TestApp"), 
                false), 
                Times.Once);
        }

        [Fact]
        public async Task RunAsync_DryRun_ShouldPassFlagToService()
        {
            // Arrange
            var config = new Configuration();
            config.Apps.Add(new AppConfig { Id = "DryRunApp", Manager = "winget" });

            var engine = new Engine(
                _managers, 
                _mockDotfiles.Object, 
                _mockRegistry.Object, 
                _mockSystemSettings.Object, 
                _mockWsl.Object, 
                _mockGit.Object, 
                _mockEnv.Object
            );

            // Act
            // dryRun = TRUE
            await engine.RunAsync(config, true);

            // Assert
            // Verify that Install was called with dryRun = true
            _mockWinget.Verify(x => x.Install(
                It.Is<AppConfig>(a => a.Id == "DryRunApp"), 
                true), 
                Times.Once);
        }
    }
}