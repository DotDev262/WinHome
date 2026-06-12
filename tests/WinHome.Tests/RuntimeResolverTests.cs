using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using WinHome.Interfaces;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class RuntimeResolverTests
  {
    [Fact]
    public void Resolve_UsesPathMatch_WhenAvailable()
    {
      if (!OperatingSystem.IsWindows()) return;

      var runtimeName = "testruntime";
      var expected = "C:\\tools\\runtime\\runtime.exe";
      var output = "C:\\tools\\runtime\\runtime.txt\r\n" + expected + "\r\n";

      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns(output);
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(expected, result);
      fileSystem.Verify(fs => fs.FileExists(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("bun", Environment.SpecialFolder.LocalApplicationData, ".bun\\bin\\bun.exe")]
    [InlineData("uv", Environment.SpecialFolder.LocalApplicationData, "uv\\uv.exe")]
    [InlineData("winget", Environment.SpecialFolder.LocalApplicationData, "Microsoft\\WindowsApps\\winget.exe")]
    [InlineData("scoop", Environment.SpecialFolder.UserProfile, "scoop\\shims\\scoop.cmd")]
    [InlineData("choco", Environment.SpecialFolder.CommonApplicationData, "chocolatey\\bin\\choco.exe")]
    public void Resolve_ReturnsKnownInstallPath_WhenNotInPath(string runtimeName, Environment.SpecialFolder baseFolder, string relativePath)
    {
      if (!OperatingSystem.IsWindows()) return;

      var expectedPath = Path.Combine(Environment.GetFolderPath(baseFolder), relativePath);

      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns(string.Empty);
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);
      fileSystem
          .Setup(fs => fs.FileExists(expectedPath))
          .Returns(true);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(expectedPath, result);
      fileSystem.Verify(fs => fs.FileExists(expectedPath), Times.Once);
    }

    [Fact]
    public void Resolve_ReturnsChocoAltPath_WhenPrimaryMissing()
    {
      if (!OperatingSystem.IsWindows()) return;

      var runtimeName = "choco";
      var primaryPath = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "chocolatey",
          "bin",
          "choco.exe");
      var expectedPath = @"C:\ProgramData\chocolatey\bin\choco.exe";

      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns(string.Empty);
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);
      fileSystem
          .Setup(fs => fs.FileExists(expectedPath))
          .Returns(true);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(expectedPath, result);
      fileSystem.Verify(fs => fs.FileExists(primaryPath), Times.Once);
      fileSystem.Verify(fs => fs.FileExists(expectedPath), Times.Once);
    }

    [Fact]
    public void Resolve_PrefersScoopExeShim_WhenPresent()
    {
      if (!OperatingSystem.IsWindows()) return;

      var runtimeName = "testruntime";
      var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      var expectedPath = Path.Combine(userProfile, "scoop", "shims", runtimeName + ".exe");

      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns(string.Empty);
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);
      fileSystem
          .Setup(fs => fs.FileExists(expectedPath))
          .Returns(true);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(expectedPath, result);
      fileSystem.Verify(fs => fs.FileExists(expectedPath), Times.Once);
    }

    [Fact]
    public void Resolve_UsesScoopCmdShim_WhenExeMissing()
    {
      if (!OperatingSystem.IsWindows()) return;

      var runtimeName = "testruntime";
      var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      var expectedPath = Path.Combine(userProfile, "scoop", "shims", runtimeName + ".cmd");

      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns(string.Empty);
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);
      fileSystem
          .Setup(fs => fs.FileExists(expectedPath))
          .Returns(true);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(expectedPath, result);
      fileSystem.Verify(fs => fs.FileExists(expectedPath), Times.Once);
    }

    [Fact]
    public void Resolve_ReturnsRuntimeName_WhenNotFound()
    {
      if (!OperatingSystem.IsWindows()) return;

      var runtimeName = "testruntime";
      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns(string.Empty);
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(runtimeName, result);
    }

    [Fact]
    public void Resolve_IgnoresNonPathWhereOutput()
    {
      if (!OperatingSystem.IsWindows()) return;

      var runtimeName = "testruntime";
      var processRunner = new Mock<IProcessRunner>();
      var fileSystem = new Mock<IFileSystem>();

      processRunner
          .Setup(r => r.RunCommandWithOutput("where.exe", It.IsAny<IEnumerable<string>>()))
          .Returns("INFO: Could not find files for the given pattern(s).");
      fileSystem
          .Setup(fs => fs.FileExists(It.IsAny<string>()))
          .Returns(false);

      var resolver = CreateResolver(processRunner, fileSystem);

      var result = resolver.Resolve(runtimeName);

      Assert.Equal(runtimeName, result);
    }

    private static RuntimeResolver CreateResolver(Mock<IProcessRunner> processRunner, Mock<IFileSystem> fileSystem)
    {
      return new RuntimeResolver(new Mock<ILogger>().Object, processRunner.Object, fileSystem.Object);
    }

  }
}
