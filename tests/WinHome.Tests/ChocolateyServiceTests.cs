using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class ChocolateyServiceTests
    {
        [Fact]
        public void Uninstall_CallsProcessRunnerWithCorrectArguments()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var chocolateyService = new ChocolateyService(mockProcessRunner.Object);
            string appId = "testapp";
            bool dryRun = false;

            // Mock the behavior of RunCommand to return true (successful execution)
            mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun))
                             .Returns(true);

            // Act
            chocolateyService.Uninstall(appId, dryRun);

            // Assert
            mockProcessRunner.Verify(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun), Times.Once);
        }

        [Fact]
        public void Uninstall_DryRun_DoesNotCallProcessRunner()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var chocolateyService = new ChocolateyService(mockProcessRunner.Object);
            string appId = "testapp";
            bool dryRun = true;

            // Act
            chocolateyService.Uninstall(appId, dryRun);

            // Assert
            mockProcessRunner.Verify(pr => pr.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var chocolateyService = new ChocolateyService(mockProcessRunner.Object);
            string appId = "testapp";
            bool dryRun = false;

            // Mock the behavior of RunCommand to return false (failed execution)
            mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"uninstall {appId} -y", dryRun))
                             .Returns(false);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => chocolateyService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to uninstall {appId} using Chocolatey.", ex.Message);
        }

        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var chocolateyService = new ChocolateyService(mockProcessRunner.Object);
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            mockProcessRunner.Setup(pr => pr.RunCommand("choco", $"install {app.Id} -y", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => chocolateyService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Chocolatey.", ex.Message);
        }
    }
}
