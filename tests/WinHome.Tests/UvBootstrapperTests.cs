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
            _mockProcessRunner.Setup(pr => pr.RunCommand("uv", "--version", false, It.IsAny<Action<string>>())).Returns(true);

            // Act
            bool isInstalled = _uvBootstrapper.IsInstalled();

            // Assert
            Assert.True(isInstalled);
        }

        [Fact]
        public void IsInstalled_ReturnsFalse_WhenUvIsNotAvailable()
        {
            // Arrange
            _mockProcessRunner.Setup(pr => pr.RunCommand("uv", "--version", false, It.IsAny<Action<string>>())).Returns(false);

            // Act
            bool isInstalled = _uvBootstrapper.IsInstalled();

            // Assert
            Assert.False(isInstalled);
        }

        // Note: The Install method uses Process.Start directly, which is hard to unit test without wrapping Process.Start.
        // However, looking at the code, it spawns powershell. 
        // We can't easily mock Process.Start unless IProcessRunner was used for installation too, or if Process creation was abstracted.
        // The current implementation of UvBootstrapper.Install uses Process.Start directly.
        // This is a design limitation. Ideally UvBootstrapper should use IProcessRunner for installation as well.
        
        // For now, I will skip testing Install method that calls Process.Start, or I should refactor UvBootstrapper to use IProcessRunner for Install as well.
        // Given "update the tests to its current status", I should try to test what I can.
        // Refactoring might be out of scope unless necessary. 
        // Wait, ChocolateyService uses IProcessRunner for everything.
        
        // Let's check UvBootstrapper again.
        // It does: using var process = Process.Start(psi);
        
        // If I want to test Install, I'd need to refactor. But the user asked to "update tests".
        // I'll stick to testing IsInstalled for now.
    }
}
