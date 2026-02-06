using System;
using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Managers;

namespace WinHome.Tests
{
    public class ScoopServiceTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<IPackageManagerBootstrapper> _mockBootstrapper;
        private readonly Mock<IRuntimeResolver> _mockRuntimeResolver;
        private readonly ScoopService _scoopService;

        public ScoopServiceTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockBootstrapper = new Mock<IPackageManagerBootstrapper>();
            _mockRuntimeResolver = new Mock<IRuntimeResolver>();
            var mockLogger = new Mock<ILogger>();

            _mockRuntimeResolver.Setup(r => r.Resolve("scoop")).Returns("scoop.cmd");

            _scoopService = new ScoopService(_mockProcessRunner.Object, _mockBootstrapper.Object, mockLogger.Object, _mockRuntimeResolver.Object);
        }

        [Fact]
        public void Install_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("scoop", "--version", false, It.IsAny<Action<string>>())).Returns(true); // IsInstalled check
            _mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed
            _mockProcessRunner.Setup(pr => pr.RunCommand("scoop.cmd", $"install {app.Id}", dryRun, It.IsAny<Action<string>>()))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _scoopService.Install(app, dryRun));
            Assert.Equal($"Failed to install {app.Id} using Scoop.", ex.Message);
        }

        [Fact]
        public void Install_Succeeds_WhenAlreadyInstalledErrorOccurs()
        {
            // Arrange
            var app = new AppConfig { Id = "testapp" };
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("scoop", "--version", false, It.IsAny<Action<string>>())).Returns(true);
            _mockProcessRunner.Setup(pr => pr.RunCommandWithOutput(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(""); // Not installed

            _mockProcessRunner.Setup(pr => pr.RunCommand("scoop.cmd", $"install {app.Id}", dryRun, It.IsAny<Action<string>>()))
                             .Callback<string, string, bool, Action<string>>((_, _, _, onOutput) =>
                             {
                                 onOutput?.Invoke($"ERROR '{app.Id}' is already installed.");
                             })
                             .Returns(false); // Fails

            // Act & Assert
            // Should not throw
            _scoopService.Install(app, dryRun);
        }

        [Fact]
        public void Uninstall_ThrowsException_WhenProcessRunnerFails()
        {
            // Arrange
            string appId = "testapp";
            bool dryRun = false;

            _mockProcessRunner.Setup(pr => pr.RunCommand("scoop", "--version", false, It.IsAny<Action<string>>())).Returns(true); // IsInstalled check
            _mockProcessRunner.Setup(pr => pr.RunCommand("scoop.cmd", $"uninstall {appId}", dryRun, It.IsAny<Action<string>>()))
                             .Returns(false); // Fails

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _scoopService.Uninstall(appId, dryRun));
            Assert.Equal($"Failed to uninstall {appId} using Scoop.", ex.Message);
        }
    }
}
