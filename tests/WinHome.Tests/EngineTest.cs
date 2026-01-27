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
        private readonly Mock<IWindowsServiceManager> _mockServiceManager;
        private readonly Mock<IScheduledTaskService> _mockScheduledTaskService;
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
            _mockServiceManager = new Mock<IWindowsServiceManager>();
            _mockScheduledTaskService = new Mock<IScheduledTaskService>();

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
                _mockEnv.Object,
                _mockServiceManager.Object,
                _mockScheduledTaskService.Object
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
                _mockEnv.Object,
                _mockServiceManager.Object,
                _mockScheduledTaskService.Object
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
        [Fact]
        public async Task PrintDiffAsync_ShouldPrintCorrectDiff()
        {
            // Arrange
            var config = new Configuration();
            config.Apps.Add(new AppConfig { Id = "UnchangedApp", Manager = "winget" });
            config.Apps.Add(new AppConfig { Id = "NewApp", Manager = "winget" });

            var engine = new Engine(
                _managers,
                _mockDotfiles.Object,
                _mockRegistry.Object,
                _mockSystemSettings.Object,
                _mockWsl.Object,
                _mockGit.Object,
                _mockEnv.Object,
                _mockServiceManager.Object,
                _mockScheduledTaskService.Object
            );
            
            var previousState = new HashSet<string> { "winget:UnchangedApp", "winget:OldApp" };
            File.WriteAllText("winhome.state.json", System.Text.Json.JsonSerializer.Serialize(previousState));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            await engine.PrintDiffAsync(config);

            // Assert
            var outputString = output.ToString();
            Assert.Contains("[-] Items to Remove:", outputString);
            Assert.Contains("- winget:OldApp", outputString);
            Assert.Contains("[+] Items to Add:", outputString);
            Assert.Contains("+ winget:NewApp", outputString);
            Assert.Contains("[=] Unchanged Items:", outputString);
            Assert.Contains("= winget:UnchangedApp", outputString);

            // Cleanup
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
            File.Delete("winhome.state.json");
        }
    }
}