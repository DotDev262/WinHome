using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class GitServiceTests
    {
        private readonly Mock<IProcessRunner> _processRunnerMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly GitService _gitService;

        public GitServiceTests()
        {
            _processRunnerMock = new Mock<IProcessRunner>();
            _loggerMock = new Mock<ILogger>();
            _gitService = new GitService(_processRunnerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Configure_GitNotInstalled_LogsError()
        {
            // Arrange
            var config = new GitConfig();
            _processRunnerMock.Setup(p => p.RunCommand("git", "--version", true)).Returns(false);

            // Act
            _gitService.Configure(config, false);

            // Assert
            _loggerMock.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Git is not installed"))), Times.Once);
        }

        [Fact]
        public void Configure_SetsUserNameAndEmail()
        {
            // Arrange
            var config = new GitConfig { UserName = "Test User", UserEmail = "test@example.com" };
            _processRunnerMock.Setup(p => p.RunCommand("git", "--version", true)).Returns(true);
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("git", "config --global --get user.name")).Returns("");
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("git", "config --global --get user.email")).Returns("");

            // Act
            _gitService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("git", "config --global user.name \"Test User\"", false), Times.Once);
            _processRunnerMock.Verify(p => p.RunCommand("git", "config --global user.email \"test@example.com\"", false), Times.Once);
        }

        [Fact]
        public void Configure_DryRun_LogsWarning()
        {
            // Arrange
            var config = new GitConfig { UserName = "Test User" };
            _processRunnerMock.Setup(p => p.RunCommand("git", "--version", true)).Returns(true);
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("git", "config --global --get user.name")).Returns("");

            // Act
            _gitService.Configure(config, true);

            // Assert
            _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Would set git config"))), Times.Once);
            _processRunnerMock.Verify(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void Configure_ValueAlreadySet_DoesNothing()
        {
            // Arrange
            var config = new GitConfig { UserName = "Test User" };
            _processRunnerMock.Setup(p => p.RunCommand("git", "--version", true)).Returns(true);
            _processRunnerMock.Setup(p => p.RunCommandWithOutput("git", "config --global --get user.name")).Returns("Test User");

            // Act
            _gitService.Configure(config, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("git", "config --global user.name \"Test User\"", false), Times.Never);
        }
    }
}
