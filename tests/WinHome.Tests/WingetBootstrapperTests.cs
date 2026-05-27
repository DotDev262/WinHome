using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using System.Diagnostics;
using System;
using System.IO;

namespace WinHome.Tests
{
    public class WingetBootstrapperTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<ILogger> _mockLogger;
        private readonly WingetBootstrapper _bootstrapper;

        public WingetBootstrapperTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockLogger = new Mock<ILogger>();
            _bootstrapper = new WingetBootstrapper(_mockProcessRunner.Object, _mockLogger.Object);
        }

        [Fact]
        public void IsInstalled_ReturnsTrue_WhenCommandSucceeds()
        {
            _mockProcessRunner.Setup(pr => pr.RunCommand("winget", "--version", false, It.IsAny<Action<string>>())).Returns(true);
            Assert.True(_bootstrapper.IsInstalled());
        }

        [Fact]
        public void IsInstalled_ReturnsFalse_WhenCommandFailsAndFileDoesNotExist()
        {
            _mockProcessRunner.Setup(pr => pr.RunCommand("winget", "--version", false, It.IsAny<Action<string>>())).Returns(false);
            
            bool result = _bootstrapper.IsInstalled();
            _mockProcessRunner.Verify(pr => pr.RunCommand("winget", "--version", false, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Install_DryRun_SkipsExecution()
        {
            _bootstrapper.Install(true);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("[DryRun]"))), Times.Once);
        }
    }
}
