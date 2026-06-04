using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using System.Diagnostics;

namespace WinHome.Tests
{
  public class UvBootstrapperTests
  {
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly UvBootstrapper _uvBootstrapper;

    public UvBootstrapperTests()
    {
      _mockProcessRunner = new Mock<IProcessRunner>();
      _uvBootstrapper = new UvBootstrapper(_mockProcessRunner.Object);
    }

    [Fact]
    public void IsInstalled_ReturnsTrue_WhenUvIsAvailable()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunCommand("uv", It.IsAny<IEnumerable<string>>(), false, It.IsAny<Action<string>?>())).Returns(true);

      // Act
      bool isInstalled = _uvBootstrapper.IsInstalled();

      // Assert
      Assert.True(isInstalled);
    }

    [Fact]
    public void IsInstalled_ReturnsFalse_WhenUvIsNotAvailable()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunCommand("uv", It.IsAny<IEnumerable<string>>(), false, It.IsAny<Action<string>?>())).Returns(false);

      // Act
      bool isInstalled = _uvBootstrapper.IsInstalled();

      // Assert
      Assert.False(isInstalled);
    }

    [Fact]
    public void Install_SuccessfulInstall_CallsProcessRunner()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>())).Returns(true);

      // Act
      _uvBootstrapper.Install(false);

      // Assert
      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.Is<ProcessStartInfo>(psi =>
              psi.FileName.Contains("scoop") &&
              psi.Arguments.Contains("install") &&
              psi.Arguments.Contains("uv"))),
          Times.Once);
    }

    [Fact]
    public void Install_FailureHandling_ThrowsException()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()))
          .Throws(new Exception("Installation failed"));

      // Act & Assert
      Assert.Throws<Exception>(() => _uvBootstrapper.Install(false));

      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()),
          Times.Once);
    }

    [Fact]
    public void Install_DryRun_SkipsExecution()
    {
      // Act
      _uvBootstrapper.Install(true);

      // Assert
      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()),
          Times.Never);
    }

    [Fact]
    public void Install_CalledMultipleTimes_IsIdempotent()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>())).Returns(true);

      // Act
      _uvBootstrapper.Install(false);
      _uvBootstrapper.Install(false);
      _uvBootstrapper.Install(false);

      // Assert
      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()),
          Times.Exactly(3));
    }

    [Fact]
    public void Name_ReturnsUv()
    {
      // Act
      string name = _uvBootstrapper.Name;

      // Assert
      Assert.Equal("uv", name);
    }
  }
}
