using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class EnvironmentServiceTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly EnvironmentService _environmentService;

        public EnvironmentServiceTests()
        {
            _loggerMock = new Mock<ILogger>();
            _environmentService = new EnvironmentService(_loggerMock.Object);
        }

        [Fact]
        public void Apply_SetVariable()
        {
            // Arrange
            var envVar = "TEST_VAR";
            var envValue = "test_value";
            var config = new EnvVarConfig { Variable = envVar, Value = envValue, Action = "set" };
            Environment.SetEnvironmentVariable(envVar, null, EnvironmentVariableTarget.User);

            // Act
            _environmentService.Apply(config, false);

            // Assert
            Assert.Equal(envValue, Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.User));
            _loggerMock.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("Set User variable"))), Times.Once);

            // Cleanup
            Environment.SetEnvironmentVariable(envVar, null, EnvironmentVariableTarget.User);
        }

        [Fact]
        public void Apply_AppendToVariable()
        {
            // Arrange
            var envVar = "TEST_PATH";
            var initialValue = "C:\\initial";
            var appendValue = "C:\\appended";
            var config = new EnvVarConfig { Variable = envVar, Value = appendValue, Action = "append" };
            Environment.SetEnvironmentVariable(envVar, initialValue, EnvironmentVariableTarget.User);

            // Act
            _environmentService.Apply(config, false);

            // Assert
            Assert.Equal($"{initialValue};{appendValue}", Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.User));

            // Cleanup
            Environment.SetEnvironmentVariable(envVar, null, EnvironmentVariableTarget.User);
        }

        [Fact]
        public void Apply_AppendToVariable_AlreadyExists_Skips()
        {
            // Arrange
            var envVar = "TEST_PATH";
            var initialValue = "C:\\initial;C:\\existing";
            var appendValue = "C:\\existing";
            var config = new EnvVarConfig { Variable = envVar, Value = appendValue, Action = "append" };
            Environment.SetEnvironmentVariable(envVar, initialValue, EnvironmentVariableTarget.User);

            // Act
            _environmentService.Apply(config, false);

            // Assert
            Assert.Equal(initialValue, Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.User));
            _loggerMock.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("already in"))), Times.Once);

            // Cleanup
            Environment.SetEnvironmentVariable(envVar, null, EnvironmentVariableTarget.User);
        }

        [Fact]
        public void Apply_SetVariable_AlreadySet_Skips()
        {
            // Arrange
            var envVar = "TEST_VAR";
            var envValue = "test_value";
            var config = new EnvVarConfig { Variable = envVar, Value = envValue, Action = "set" };
            Environment.SetEnvironmentVariable(envVar, envValue, EnvironmentVariableTarget.User);

            // Act
            _environmentService.Apply(config, false);

            // Assert
            _loggerMock.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("is already correct"))), Times.Once);
            _loggerMock.Verify(l => l.LogSuccess(It.IsAny<string>()), Times.Never);

            // Cleanup
            Environment.SetEnvironmentVariable(envVar, null, EnvironmentVariableTarget.User);
        }

        [Fact]
        public void Apply_DryRun_LogsWarning()
        {
            // Arrange
            var envVar = "TEST_VAR";
            var envValue = "test_value";
            var config = new EnvVarConfig { Variable = envVar, Value = envValue, Action = "set" };
            Environment.SetEnvironmentVariable(envVar, null, EnvironmentVariableTarget.User);

            // Act
            _environmentService.Apply(config, true);

            // Assert
            Assert.Null(Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.User));
            _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Would set User Env Var"))), Times.Once);
        }
    }
}
