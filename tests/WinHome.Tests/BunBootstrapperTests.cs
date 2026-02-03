using Moq;
using Xunit;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;

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
            _mockProcessRunner.Setup(pr => pr.RunCommand("bun", "--version", false, It.IsAny<Action<string>>())).Returns(true);

            // Act
            bool isInstalled = _bunBootstrapper.IsInstalled();

            // Assert
            Assert.True(isInstalled);
        }

        [Fact]
        public void IsInstalled_ReturnsFalse_WhenBunIsNotAvailable()
        {
            // Arrange
            _mockProcessRunner.Setup(pr => pr.RunCommand("bun", "--version", false, It.IsAny<Action<string>>())).Returns(false);

            // Act
            bool isInstalled = _bunBootstrapper.IsInstalled();

            // Assert
            Assert.False(isInstalled);
        }
    }
}
