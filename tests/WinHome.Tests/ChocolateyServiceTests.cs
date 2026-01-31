using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class ChocolateyServiceTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<IPackageManagerBootstrapper> _mockBootstrapper;
        private readonly ChocolateyService _chocolateyService;

        public ChocolateyServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockBootstrapper = new Mock<IPackageManagerBootstrapper>();
            _chocolateyService = new ChocolateyService(_mockProcessRunner.Object, _mockBootstrapper.Object);
        }

        [Fact]
        public void Uninstall_CallsProcessRunnerWithCorrectArguments()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun))
                             .Returns(true);

            // Act
            _chocolateyService.Uninstall(appId, dryRun);

            // Assert
            _mockProcessRunner.Verify(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun), Times.Once);
        }

        [Fact]
        public void Uninstall_DryRun_DoesNotCallProcessRunner()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = true;

            // Act
            _chocolateyService.Uninstall(appId, dryRun);

            // Assert
            _mockProcessRunner.Verify(pr => pr.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun))
                             .Returns(false);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _chocolateyService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to uninstall {appId} using Chocolatey.", ex.Message);
        }

        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"install {app.Id} -y", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _chocolateyService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Chocolatey.", ex.Message);
        }
    }
}
