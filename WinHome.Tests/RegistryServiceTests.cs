using Microsoft.Win32;
using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class RegistryServiceTests
    {
        private readonly Mock<IRegistryWrapper> _mockRegistryWrapper;
        private readonly RegistryService _registryService;
        private readonly Mock<IRegistryKey> _mockRegistryKey;

        public delegate void GetRootKeyCallback(string fullPath, out string subKey);

        public RegistryServiceTests()
        {
            _mockRegistryWrapper = new Mock<IRegistryWrapper>();
            _mockRegistryKey = new Mock<IRegistryKey>();
            _registryService = new RegistryService(_mockRegistryWrapper.Object);
        }

        [Fact]
        public void Apply_Should_Set_Registry_Value()
        {
            // Arrange
            var tweak = new RegistryTweak { Path = "HKCU\\Software\\Test", Name = "TestValue", Value = "Test", Type = "string" };
            var subKeyPath = "Software\\Test";
            _mockRegistryWrapper.Setup(x => x.GetRootKey(tweak.Path, out subKeyPath))
                .Callback(new GetRootKeyCallback((string fullPath, out string s) => s = subKeyPath))
                .Returns(_mockRegistryKey.Object);
            _mockRegistryKey.Setup(x => x.OpenSubKey(It.IsAny<string>(), It.IsAny<bool>())).Returns(_mockRegistryKey.Object);
            _mockRegistryKey.Setup(x => x.GetValue(tweak.Name)).Returns((object?)null);
            _mockRegistryKey.Setup(x => x.CreateSubKey(It.IsAny<string>(), It.IsAny<bool>())).Returns(_mockRegistryKey.Object);


            // Act
            _registryService.Apply(tweak, false);

            // Assert
            _mockRegistryKey.Verify(x => x.SetValue(tweak.Name, tweak.Value, RegistryValueKind.String), Times.Once);
        }

        [Fact]
        public void Revert_Should_Delete_Registry_Value()
        {
            // Arrange
            var path = "HKCU\\Software\\Test";
            var name = "TestValue";
            var subKeyPath = "Software\\Test";
            _mockRegistryWrapper.Setup(x => x.GetRootKey(path, out subKeyPath))
                 .Callback(new GetRootKeyCallback((string fullPath, out string s) => s = subKeyPath))
                .Returns(_mockRegistryKey.Object);
            _mockRegistryKey.Setup(x => x.OpenSubKey(It.IsAny<string>(), It.IsAny<bool>())).Returns(_mockRegistryKey.Object);
            _mockRegistryKey.Setup(x => x.GetValue(name)).Returns("some value");

            // Act
            _registryService.Revert(path, name, false);

            // Assert
            _mockRegistryKey.Verify(x => x.DeleteValue(name), Times.Once);
        }
    }
}
