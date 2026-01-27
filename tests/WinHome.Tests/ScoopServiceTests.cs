using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class ScoopServiceTests
    {
        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var scoopService = new ScoopService(mockProcessRunner.Object);
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            mockProcessRunner.Setup(pr => pr.RunCommand("scoop.cmd", $"install {app.Id}", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => scoopService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Scoop.", ex.Message);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var scoopService = new ScoopService(mockProcessRunner.Object);
            string appId = "testapp";
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommand("scoop.cmd", $"uninstall {appId}", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => scoopService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to uninstall {appId} using Scoop.", ex.Message);
        }
    }
}
