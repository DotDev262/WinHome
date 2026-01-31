using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class WingetServiceTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<IPackageManagerBootstrapper> _mockBootstrapper;
        private readonly WingetService _wingetService;

        public WingetServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockBootstrapper = new Mock<IPackageManagerBootstrapper>();
            var mockLogger = new Mock<ILogger>();
            _wingetService = new WingetService(_mockProcessRunner.Object, _mockBootstrapper.Object, mockLogger.Object);
        }

        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            _mockProcessRunner.Setup(pr => pr.RunCommand("winget", $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _wingetService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Winget.", ex.Message);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("winget", $"uninstall --id {appId} -e --silent --accept-source-agreements", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _wingetService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to uninstall {appId} using Winget.", ex.Message);
        }
    }
}
