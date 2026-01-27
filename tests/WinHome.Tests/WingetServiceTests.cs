using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class WingetServiceTests
    {
        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var wingetService = new WingetService(mockProcessRunner.Object);
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            mockProcessRunner.Setup(pr => pr.RunCommand("winget", $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => wingetService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Winget.", ex.Message);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var wingetService = new WingetService(mockProcessRunner.Object);
            string appId = "testapp";
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommand("winget", $"uninstall --id {appId} -e --silent --accept-source-agreements", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => wingetService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to uninstall {appId} using Winget.", ex.Message);
        }
    }
}
