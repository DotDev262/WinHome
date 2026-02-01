using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class WslServiceTests
    {
        private readonly Mock<IProcessRunner> _processRunnerMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly WslService _wslService;

        public WslServiceTests()
        {
            _processRunnerMock = new Mock<IProcessRunner>();
            _loggerMock = new Mock<ILogger>();
            _wslService = new WslService(_processRunnerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Configure_WslNotInstalled_LogsError()
        {
            // Arrange
            var config = new WslConfig();
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(false);

            // Act
            _wslService.Configure(config, false);

            // Assert
            _loggerMock.Verify(l => l.LogError(It.Is<string>(s => s.Contains("WSL is not active"))), Times.Once);
        }

        [Fact]
        public void Configure_WslNotInstalled_DryRun_LogsWarning()
        {
            // Arrange
            var config = new WslConfig();
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(false);

            // Act
            _wslService.Configure(config, true);

            // Assert
            _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("WSL is not detected/active"))), Times.Once);
        }

        [Fact]
        public void Configure_UpdateWsl_CallsUpdateCommand()
        {
            // Arrange
            var config = new WslConfig { Update = true };
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(true);

            // Act
            _wslService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("wsl", "--update", false, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Configure_SetDefaultVersion_CallsSetDefaultVersionCommand()
        {
            // Arrange
            var config = new WslConfig { DefaultVersion = 2 };
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(true);

            // Act
            _wslService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("wsl", "--set-default-version 2", false, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Configure_InstallDistro_IfNotInstalled()
        {
            // Arrange
            var config = new WslConfig();
            config.Distros.Add(new WslDistroConfig { Name = "Ubuntu" });
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(true);
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("wsl", "--list --verbose")).Returns("");

            // Act
            _wslService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("wsl", "--install -d Ubuntu", false, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Configure_SetDefaultDistro()
        {
            // Arrange
            var config = new WslConfig { DefaultDistro = "Ubuntu" };
            config.Distros.Add(new WslDistroConfig { Name = "Ubuntu" });
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(true);
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("wsl", "--list --verbose")).Returns("Ubuntu");

            // Act
            _wslService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("wsl", "--set-default Ubuntu", false, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Configure_ProvisionDistro_WithSetupScript()
        {
            // Arrange
            var scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, "echo 'hello'");
            var config = new WslConfig();
            config.Distros.Add(new WslDistroConfig { Name = "Ubuntu", SetupScript = scriptPath });
            _processRunnerMock.Setup(p => p.RunCommand("wsl", "--status", false, It.IsAny<Action<string>>())).Returns(true);
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("wsl", "--list --verbose")).Returns("Ubuntu");
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("wsl", $"-d Ubuntu -- bash -s", "echo 'hello'")).Returns("hello");

            // Act
            _wslService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommandWithOutput("wsl", $"-d Ubuntu -- bash -s", "echo 'hello'"), Times.Once);
            _loggerMock.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("hello"))), Times.Once);

            // Cleanup
            File.Delete(scriptPath);
        }
    }
}
