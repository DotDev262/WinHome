using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class MiseServiceTests
    {
        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var miseService = new MiseService(mockProcessRunner.Object);
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            mockProcessRunner.Setup(pr => pr.RunCommand("mise", $"use --global {app.Id} -y", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => miseService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Mise.", ex.Message);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var mockProcessRunner = new Mock<IProcessRunner>();
            var miseService = new MiseService(mockProcessRunner.Object);
            string appId = "testapp";
            bool dryRun = false;

            mockProcessRunner.Setup(pr => pr.RunCommand("mise", $"unuse --global {appId}", dryRun))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => miseService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to remove {appId} using Mise.", ex.Message);
        }
    }
}
