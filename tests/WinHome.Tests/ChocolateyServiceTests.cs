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
            var mockLogger = new Mock<ILogger>();
            _chocolateyService = new ChocolateyService(_mockProcessRunner.Object, _mockBootstrapper.Object, mockLogger.Object);
        }

        [Fact]
        public void Uninstall_CallsProcessRunnerWithCorrectArguments()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun, It.IsAny<Action<string>>()))
                             .Returns(true);

            // Act
            _chocolateyService.Uninstall(appId, dryRun);

            // Assert
            _mockProcessRunner.Verify(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Uninstall_DryRun_DoesNotCallProcessRunner()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = true;

            // Allow version checks
            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", "--version", false, It.IsAny<Action<string>>())).Returns(true);

            // Act
            _chocolateyService.Uninstall(appId, dryRun);

            // Assert
            _mockProcessRunner.Verify(pr => pr.RunCommand("choco", $"uninstall {appId} -y", It.IsAny<bool>(), It.IsAny<Action<string>>()), Times.Never);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun, It.IsAny<Action<string>>()))
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
            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"install {app.Id} -y", dryRun, It.IsAny<Action<string>>()))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _chocolateyService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Chocolatey.", ex.Message);
        }

        [Fact]
        public void Install_Succeeds_WhenAlreadyInstalledErrorOccurs()
        {
            // Arrange
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"install {app.Id} -y", dryRun, It.IsAny<Action<string>>()))
                             .Callback<string, string, bool, Action<string>>((_, _, _, onOutput) =>
                             {
                                 onOutput?.Invoke("Chocolatey installed 0/1 packages. ... 1 packages installed currently");
                             })
                             .Returns(false); // Fails (hypothetically, if choco decides to return non-zero)

            // Act & Assert
            // Should not throw
            _chocolateyService.Install(app, dryRun);
        }
    }
}
