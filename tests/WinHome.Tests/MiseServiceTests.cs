using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class MiseServiceTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<IPackageManagerBootstrapper> _mockBootstrapper;
        private readonly MiseService _miseService;

        public MiseServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockBootstrapper = new Mock<IPackageManagerBootstrapper>();
            var mockLogger = new Mock<ILogger>();
            _miseService = new MiseService(_mockProcessRunner.Object, _mockBootstrapper.Object, mockLogger.Object);
        }

        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            _mockProcessRunner.Setup(pr => pr.RunCommand("mise", $"use --global {app.Id} -y", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _miseService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Mise.", ex.Message);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("mise", $"unuse --global {appId}", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _miseService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to remove {appId} using Mise.", ex.Message);
        }
    }
}
