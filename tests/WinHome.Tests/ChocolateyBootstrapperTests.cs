using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using System.Diagnostics;
using System;
using System.IO;

namespace WinHome.Tests
{
    public class ChocolateyBootstrapperTests
    {
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly ChocolateyBootstrapper _bootstrapper;

        public ChocolateyBootstrapperTests()
        {
            _mockProcessRunner = new Mock<IProcessRunner>();
            _bootstrapper = new ChocolateyBootstrapper(_mockProcessRunner.Object);
        }

        [Fact]
        public void IsInstalled_ReturnsTrue_WhenCommandSucceeds()
        {
            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", "--version", false, It.IsAny<Action<string>>())).Returns(true);
            Assert.True(_bootstrapper.IsInstalled());
        }

        [Fact]
        public void IsInstalled_WhenCommandFails_StillChecksChocolateyVersionCommand()
        {
            _mockProcessRunner.Setup(pr => pr.RunCommand("choco", "--version", false, It.IsAny<Action<string>>())).Returns(false);
            
            // Note: Since File.Exists fallback is hard to mock reliably across systems,
            // we at least ensure RunCommand is called. If the fallback file exists on the CI
            // machine, this test might incorrectly return true. But it's sufficient for basic coverage.
            // A more robust approach would abstract File System access.
            _bootstrapper.IsInstalled();
            _mockProcessRunner.Verify(pr => pr.RunCommand("choco", "--version", false, It.IsAny<Action<string>>()), Times.Once);
        }

        [Fact]
        public void Install_SuccessfulInstall_CallsProcessRunner()
        {
            _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>())).Returns(true);
            
            _bootstrapper.Install(false);

            _mockProcessRunner.Verify(
                pr => pr.RunProcessWithStartInfo(It.Is<ProcessStartInfo>(psi =>
                    psi.FileName.Contains("powershell") &&
                    psi.Arguments.Contains("chocolatey.org/install.ps1"))),
                Times.Once);
        }

        [Fact]
        public void Install_FailureHandling_ThrowsException()
        {
            _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()))
                .Throws(new Exception("Process failed with exit code 1: generic error"));

            var ex = Assert.Throws<Exception>(() => _bootstrapper.Install(false));
            Assert.Contains("Failed to install", ex.Message);
        }

        [Fact]
        public void Install_DryRun_SkipsExecution()
        {
            _bootstrapper.Install(true);
            _mockProcessRunner.Verify(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()), Times.Never);
        }
    }
}
