using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using WinHome.Interfaces;
using WinHome.Services.System;
using WinHome.Models;
using System.ServiceProcess; // Required for ServiceControllerStatus

namespace WinHome.Tests
{
    public class WindowsServiceManagerTests
    {
        private readonly Mock<ILogger<WindowsServiceManager>> _loggerMock;
        private readonly Mock<IProcessRunner> _processRunnerMock;
        private readonly Mock<IServiceControllerWrapper> _serviceControllerWrapperMock;
        private readonly WindowsServiceManager _serviceManager;

        public WindowsServiceManagerTests()
        {
            _loggerMock = new Mock<ILogger<WindowsServiceManager>>();
            _processRunnerMock = new Mock<IProcessRunner>();
            _serviceControllerWrapperMock = new Mock<IServiceControllerWrapper>();
            _serviceManager = new WindowsServiceManager(_loggerMock.Object, _processRunnerMock.Object, _serviceControllerWrapperMock.Object);
        }

        [Fact]
        public void Apply_ServiceExists_SetsStartupAndState()
        {
            // Arrange
            var serviceConfig = new WindowsServiceConfig { Name = "TestService", StartupType = "automatic", State = "running" };
            _serviceControllerWrapperMock.Setup(s => s.ServiceExists(serviceConfig.Name)).Returns(true);
            _serviceControllerWrapperMock.Setup(s => s.GetServiceStatus(serviceConfig.Name)).Returns(ServiceControllerStatus.Stopped); // Simulate service being stopped
            _processRunnerMock.Setup(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            // Act
            _serviceManager.Apply(serviceConfig, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("sc.exe", $"config \"{serviceConfig.Name}\" start= {serviceConfig.StartupType}", false), Times.Once);
            _serviceControllerWrapperMock.Verify(s => s.StartService(serviceConfig.Name), Times.Once);
        }

        [Fact]
        public void Apply_ServiceNotExists_LogsWarningAndSkips()
        {
            // Arrange
            var serviceConfig = new WindowsServiceConfig { Name = "NonExistentService" };
            _serviceControllerWrapperMock.Setup(s => s.ServiceExists(serviceConfig.Name)).Returns(false);

            // Act
            _serviceManager.Apply(serviceConfig, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _loggerMock.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Service '{serviceConfig.Name}' not found. Skipping.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Apply_ServiceExists_OnlySetsStartupType()
        {
            // Arrange
            var serviceConfig = new WindowsServiceConfig { Name = "TestService", StartupType = "manual" };
            _serviceControllerWrapperMock.Setup(s => s.ServiceExists(serviceConfig.Name)).Returns(true);
            _serviceControllerWrapperMock.Setup(s => s.GetServiceStatus(serviceConfig.Name)).Returns(ServiceControllerStatus.Running);
            _processRunnerMock.Setup(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            // Act
            _serviceManager.Apply(serviceConfig, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand("sc.exe", $"config \"{serviceConfig.Name}\" start= {serviceConfig.StartupType}", false), Times.Once);
            _serviceControllerWrapperMock.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
            _serviceControllerWrapperMock.Verify(s => s.StopService(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Apply_ServiceExists_OnlySetsStateToStopped()
        {
            // Arrange
            var serviceConfig = new WindowsServiceConfig { Name = "TestService", State = "stopped" };
            _serviceControllerWrapperMock.Setup(s => s.ServiceExists(serviceConfig.Name)).Returns(true);
            _serviceControllerWrapperMock.Setup(s => s.GetServiceStatus(serviceConfig.Name)).Returns(ServiceControllerStatus.Running);
            _processRunnerMock.Setup(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            // Act
            _serviceManager.Apply(serviceConfig, false);

            // Assert
            _processRunnerMock.Verify(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never); // No startup type change
            _serviceControllerWrapperMock.Verify(s => s.StopService(serviceConfig.Name), Times.Once);
            _serviceControllerWrapperMock.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Apply_ServiceExists_AlreadyInDesiredState_NoAction()
        {
            // Arrange
            var serviceConfig = new WindowsServiceConfig { Name = "TestService", State = "running", StartupType = "automatic" };
            _serviceControllerWrapperMock.Setup(s => s.ServiceExists(serviceConfig.Name)).Returns(true);
            _serviceControllerWrapperMock.Setup(s => s.GetServiceStatus(serviceConfig.Name)).Returns(ServiceControllerStatus.Running);
            // Simulate that sc.exe config also returns success if already automatic
            _processRunnerMock.Setup(p => p.RunCommand(It.IsAny<string>(), It.Is<string>(args => args.Contains("start= automatic")), It.IsAny<bool>())).Returns(true);


            // Act
            _serviceManager.Apply(serviceConfig, false);

            // Assert
            // Verify that startup type command was attempted (sc.exe does not check current state for this)
            _processRunnerMock.Verify(p => p.RunCommand("sc.exe", $"config \"{serviceConfig.Name}\" start= {serviceConfig.StartupType}", false), Times.Once);
            // But no start/stop action should be taken by the wrapper if already running
            _serviceControllerWrapperMock.Verify(s => s.StartService(It.IsAny<string>()), Times.Never);
            _serviceControllerWrapperMock.Verify(s => s.StopService(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Apply_ServiceExists_StateChangeFails_LogsError()
        {
            // Arrange
            var serviceConfig = new WindowsServiceConfig { Name = "TestService", State = "running" };
            _serviceControllerWrapperMock.Setup(s => s.ServiceExists(serviceConfig.Name)).Returns(true);
            _serviceControllerWrapperMock.Setup(s => s.GetServiceStatus(serviceConfig.Name)).Returns(ServiceControllerStatus.Stopped);
            _serviceControllerWrapperMock.Setup(s => s.StartService(serviceConfig.Name)).Throws(new InvalidOperationException("Failed to start"));

            // Act
            _serviceManager.Apply(serviceConfig, false);

            // Assert
            _serviceControllerWrapperMock.Verify(s => s.StartService(serviceConfig.Name), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to start service '{serviceConfig.Name}': Failed to start")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
