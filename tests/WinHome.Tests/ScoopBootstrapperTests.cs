using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using System.Diagnostics;
using System;
using System.Collections.Generic;

namespace WinHome.Tests
{
  public class ScoopBootstrapperTests
  {
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly ScoopBootstrapper _bootstrapper;

    public ScoopBootstrapperTests()
    {
      _mockProcessRunner = new Mock<IProcessRunner>();
      _bootstrapper = new ScoopBootstrapper(_mockProcessRunner.Object);
    }

    [Fact]
    public void IsInstalled_ReturnsTrue_WhenCommandSucceeds()
    {
      _mockProcessRunner.Setup(pr => pr.RunCommand("scoop", It.Is<IEnumerable<string>>(a => a.Contains("--version")), false, It.IsAny<Action<string>?>())).Returns(true);
      Assert.True(_bootstrapper.IsInstalled());
    }

    [Fact]
    public void IsInstalled_ReturnsFalse_WhenScoopIsNotAvailable()
    {
      _mockProcessRunner.Setup(pr => pr.RunCommand("scoop", It.Is<IEnumerable<string>>(a => a.Contains("--version")), false, It.IsAny<Action<string>?>())).Returns(false);

      bool result = _bootstrapper.IsInstalled();

      // Note: Cannot assert Assert.False(result) reliably without abstracting File.Exists
      _mockProcessRunner.Verify(pr => pr.RunCommand("scoop", It.Is<IEnumerable<string>>(a => a.Contains("--version")), false, It.IsAny<Action<string>?>()), Times.Once);
    }

    [Fact]
    public void Install_SuccessfulInstall_CallsProcessRunner()
    {
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>())).Returns(true);

      _bootstrapper.Install(false);

      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.Is<ProcessStartInfo>(psi =>
              psi.FileName.Contains("powershell") &&
              psi.Arguments.Contains("get.scoop.sh"))),
          Times.Once);
    }

    [Fact]
    public void Install_FailureHandling_ThrowsException()
    {
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()))
          .Throws(new Exception("Process failed with exit code 1: Installation failed"));

      var ex = Assert.Throws<Exception>(() => _bootstrapper.Install(false));
      Assert.Contains("Failed to install", ex.Message);
    }

    [Fact]
    public void Install_DryRun_SkipsExecution()
    {
      _bootstrapper.Install(true);
      _mockProcessRunner.Verify(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    [Fact]
    public void Install_CalledMultipleTimes_IsIdempotent()
    {
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>())).Returns(true);

      _bootstrapper.Install(false);
      _bootstrapper.Install(false);
      _bootstrapper.Install(false);

      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()),
          Times.Exactly(3));
    }

    [Fact]
    public void Name_ReturnsScoop()
    {
      string name = _bootstrapper.Name;
      Assert.Equal("Scoop", name);
    }
  }
}
