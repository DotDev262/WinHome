using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using System.Diagnostics;

namespace WinHome.Tests
{
  public class BunBootstrapperTests
  {
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly BunBootstrapper _bunBootstrapper;

    public BunBootstrapperTests()
    {
      _mockProcessRunner = new Mock<IProcessRunner>();
      _bunBootstrapper = new BunBootstrapper(_mockProcessRunner.Object);
    }

    [Fact]
    public void IsInstalled_ReturnsTrue_WhenBunIsAvailable()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunCommand("bun", It.IsAny<IEnumerable<string>>(), false, It.IsAny<Action<string>?>())).Returns(true);

      // Act
      bool isInstalled = _bunBootstrapper.IsInstalled();

      // Assert
      Assert.True(isInstalled);
    }

    [Fact]
    public void IsInstalled_ReturnsFalse_WhenBunIsNotAvailable()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunCommand("bun", It.IsAny<IEnumerable<string>>(), false, It.IsAny<Action<string>?>())).Returns(false);

      // Act
      bool isInstalled = _bunBootstrapper.IsInstalled();

      // Assert
      Assert.False(isInstalled);
    }

    [Fact]
    public void Install_SuccessfulInstall_CallsProcessRunner()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>())).Returns(true);

      // Act
      _bunBootstrapper.Install(false);

      // Assert
      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.Is<ProcessStartInfo>(psi =>
              psi.FileName.Contains("scoop") &&
              psi.Arguments.Contains("install") &&
              psi.Arguments.Contains("bun"))),
          Times.Once);
    }

    [Fact]
    public void Install_FailureHandling_ThrowsException()
    {
      // Arrange
      _mockProcessRunner.Setup(pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()))
          .Throws(new Exception("Installation failed"));

      // Act & Assert
      Assert.Throws<Exception>(() => _bunBootstrapper.Install(false));

      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()),
          Times.Once);
    }

    [Fact]
    public void Install_DryRun_SkipsExecution()
    {
      // Act
      _bunBootstrapper.Install(true);

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
      _bunBootstrapper.Install(false);
      _bunBootstrapper.Install(false);
      _bunBootstrapper.Install(false);

      // Assert
      _mockProcessRunner.Verify(
          pr => pr.RunProcessWithStartInfo(It.IsAny<ProcessStartInfo>()),
          Times.Exactly(3));
    }

    [Fact]
    public void Name_ReturnsBun()
    {
      // Act
      string name = _bunBootstrapper.Name;

      // Assert
      Assert.Equal("bun", name);
    }
  }
}
