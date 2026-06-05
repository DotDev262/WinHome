using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Models;
using Xunit;

namespace WinHome.Tests
{
  public class AppRunnerTests : IDisposable
  {
    private readonly Mock<IConfigValidator> _mockValidator;
    private readonly Mock<ISecretResolver> _mockSecretResolver;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEngine> _mockEngine;
    private readonly AppRunner _appRunner;
    private readonly string _tempConfigFile;

    public AppRunnerTests()
    {
      _mockValidator = new Mock<IConfigValidator>();
      _mockSecretResolver = new Mock<ISecretResolver>();
      _mockLogger = new Mock<ILogger>();
      
      // Engine requires a lot of parameters for its constructor, we'll pass null! since we're mocking it
      _mockEngine = new Mock<IEngine>();

      _appRunner = new AppRunner(_mockEngine.Object, _mockValidator.Object, _mockSecretResolver.Object, _mockLogger.Object);

      _tempConfigFile = Path.GetTempFileName();
      File.WriteAllText(_tempConfigFile, "name: test-config\nversion: 1.0\n");
    }

    public void Dispose()
    {
      if (File.Exists(_tempConfigFile))
      {
        File.Delete(_tempConfigFile);
      }
    }

    [Fact]
    public async Task RunAsync_ReturnsOne_WhenFileNotFound()
    {
      // Arrange
      var nonExistentFile = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".yaml"));

      // Act
      var exitCode = await _appRunner.RunAsync(nonExistentFile, false, null, false, false, false);

      // Assert
      Assert.Equal(1, exitCode);
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("not found"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsOne_WhenValidationFails()
    {
      // Arrange
      _mockValidator.Setup(v => v.Validate(It.IsAny<string>())).Returns((false, new System.Collections.Generic.List<string> { "Error 1" }));

      // Act
      var exitCode = await _appRunner.RunAsync(new FileInfo(_tempConfigFile), false, null, false, false, false);

      // Assert
      Assert.Equal(1, exitCode);
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("failed"))), Times.Once);
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Error 1"))), Times.Once);
      _mockEngine.Verify(e => e.RunAsync(It.IsAny<Configuration>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_CallsEngineAndReturnsZero_OnSuccess()
    {
      // Arrange
      _mockValidator.Setup(v => v.Validate(It.IsAny<string>())).Returns((true, new System.Collections.Generic.List<string>()));
      _mockEngine.Setup(e => e.RunAsync(It.IsAny<Configuration>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                 .Returns(Task.CompletedTask);

      // Act
      var exitCode = await _appRunner.RunAsync(new FileInfo(_tempConfigFile), dryRun: true, profile: "dev", debug: true, diff: true, json: false, force: true, continueOnError: true);

      // Assert
      Assert.Equal(0, exitCode);
      _mockSecretResolver.Verify(s => s.ResolveObject(It.IsAny<Configuration>()), Times.Once);
      _mockEngine.Verify(e => e.RunAsync(
          It.IsAny<Configuration>(),
          true, // dryRun
          "dev", // profileName
          true, // debug
          true, // diff
          true, // forceReapply
          true // continueOnError
      ), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsOne_OnException()
    {
      // Arrange
      _mockValidator.Setup(v => v.Validate(It.IsAny<string>())).Throws(new Exception("Test Exception"));

      // Act
      var exitCode = await _appRunner.RunAsync(new FileInfo(_tempConfigFile), false, null, false, false, false);

      // Assert
      Assert.Equal(1, exitCode);
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("unexpected error"))), Times.Once);
    }
    
    [Fact]
    public async Task RunAsync_LogsStackTrace_OnException_WhenDebugTrue()
    {
      // Arrange
      var exception = new Exception("Test Exception");
      _mockValidator.Setup(v => v.Validate(It.IsAny<string>())).Throws(exception);

      // Act
      var exitCode = await _appRunner.RunAsync(new FileInfo(_tempConfigFile), false, null, true, false, false);

      // Assert
      Assert.Equal(1, exitCode);
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("unexpected error"))), Times.Once);
      // The stack trace might be empty since it's just thrown here, but it verifies it was called multiple times
      _mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.AtLeast(2));
    }
  }
}
