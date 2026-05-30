using Microsoft.Win32;
using Moq;
using WinHome.Infrastructure.Helpers;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class RegistryGuardTests : IDisposable
  {
    private readonly Mock<IRegistryWrapper> _mockRegistryWrapper = new();
    private readonly Mock<IRegistryKey> _mockRegistryKey = new();

    public void Dispose()
    {
      RegistryGuard.ResetSystemUserCheck();
    }

    [Theory]
    [InlineData("HKCU\\Software\\Test")]
    [InlineData("HKEY_CURRENT_USER\\Software\\Test")]
    public void ValidateContext_Should_Block_HKCU_When_System(string path)
    {
      RegistryGuard.IsSystemUser = () => true;

      var ex = Assert.Throws<InvalidOperationException>(() =>
          RegistryGuard.ValidateContext(path));

      Assert.Contains("Security Risk", ex.Message);
    }

    [Theory]
    [InlineData("HKCU\\Software\\Test")]
    [InlineData("HKEY_CURRENT_USER\\Software\\Test")]
    public void ValidateContext_Should_Allow_HKCU_When_Normal_User(string path)
    {
      RegistryGuard.IsSystemUser = () => false;

      var exception = Record.Exception(() =>
          RegistryGuard.ValidateContext(path));

      Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("HKLM\\Software\\Test")]
    [InlineData("HKCU\\Special!@#$%^&*()")]
    public void ValidateContext_Should_Handle_Edge_Cases(string? path)
    {
      RegistryGuard.IsSystemUser = () => false;

      var exception = Record.Exception(() =>
          RegistryGuard.ValidateContext(path!));

      Assert.Null(exception);
    }

    [Fact]
    public void RegistryService_Should_Block_HKCU_Modification_When_System()
    {
      RegistryGuard.IsSystemUser = () => true;

      var service = new RegistryService(_mockRegistryWrapper.Object);

      var tweak = new RegistryTweak
      {
        Path = "HKCU\\Software\\Blocked",
        Name = "Test",
        Value = "123",
        Type = "string"
      };

      Assert.Throws<InvalidOperationException>(() =>
          service.Apply(tweak, false));

      _mockRegistryWrapper.Verify(
          x => x.GetRootKey(It.IsAny<string>(), out It.Ref<string>.IsAny),
          Times.Never);
    }

    [Fact]
    public void RegistryService_Should_Allow_HKCU_Modification_When_Normal_User()
    {
      RegistryGuard.IsSystemUser = () => false;

      var service = new RegistryService(_mockRegistryWrapper.Object);

      var tweak = new RegistryTweak
      {
        Path = "HKCU\\Software\\Allowed",
        Name = "Test",
        Value = "123",
        Type = "string"
      };

      string subKeyPath = "Software\\Allowed";

      _mockRegistryWrapper
          .Setup(x => x.GetRootKey(tweak.Path, out subKeyPath))
          .Returns(_mockRegistryKey.Object);

      _mockRegistryKey
          .Setup(x => x.OpenSubKey(It.IsAny<string>(), false))
          .Returns(_mockRegistryKey.Object);

      _mockRegistryKey
          .Setup(x => x.CreateSubKey(It.IsAny<string>(), true))
          .Returns(_mockRegistryKey.Object);

      _mockRegistryKey
          .Setup(x => x.GetValue(It.IsAny<string>()))
          .Returns((object?)null);

      service.Apply(tweak, false);

      _mockRegistryKey.Verify(
          x => x.SetValue(
              tweak.Name,
              tweak.Value,
              RegistryValueKind.String),
          Times.Once);
    }
  }
}
