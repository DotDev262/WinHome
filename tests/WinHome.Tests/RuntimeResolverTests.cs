using System;
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
            var runtimeName = "testruntime";
            var expected = "C:\\tools\\runtime\\runtime.exe";
            var output = "C:\\tools\\runtime\\runtime.txt\r\n" + expected + "\r\n";

            var processRunner = new Mock<IProcessRunner>();
            var fileSystem = new Mock<IFileSystem>();

            processRunner
                .Setup(r => r.RunCommandWithOutput("where.exe", runtimeName))
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
        [InlineData("bun")]
        [InlineData("uv")]
        [InlineData("winget")]
        [InlineData("scoop")]
        [InlineData("choco")]
        public void Resolve_ReturnsKnownInstallPath_WhenNotInPath(string runtimeName)
        {
            var expectedPath = GetKnownInstallPath(runtimeName, useChocoAlt: false);

            var processRunner = new Mock<IProcessRunner>();
            var fileSystem = new Mock<IFileSystem>();

            processRunner
                .Setup(r => r.RunCommandWithOutput("where.exe", runtimeName))
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
            var runtimeName = "choco";
            var primaryPath = GetKnownInstallPath(runtimeName, useChocoAlt: false);
            var expectedPath = GetKnownInstallPath(runtimeName, useChocoAlt: true);

            var processRunner = new Mock<IProcessRunner>();
            var fileSystem = new Mock<IFileSystem>();

            processRunner
                .Setup(r => r.RunCommandWithOutput("where.exe", runtimeName))
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
            var runtimeName = "testruntime";
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var expectedPath = Path.Combine(userProfile, "scoop", "shims", runtimeName + ".exe");

            var processRunner = new Mock<IProcessRunner>();
            var fileSystem = new Mock<IFileSystem>();

            processRunner
                .Setup(r => r.RunCommandWithOutput("where.exe", runtimeName))
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
            var runtimeName = "testruntime";
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var expectedPath = Path.Combine(userProfile, "scoop", "shims", runtimeName + ".cmd");

            var processRunner = new Mock<IProcessRunner>();
            var fileSystem = new Mock<IFileSystem>();

            processRunner
                .Setup(r => r.RunCommandWithOutput("where.exe", runtimeName))
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
            var runtimeName = "testruntime";
            var processRunner = new Mock<IProcessRunner>();
            var fileSystem = new Mock<IFileSystem>();

            processRunner
                .Setup(r => r.RunCommandWithOutput("where.exe", runtimeName))
                .Returns(string.Empty);
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

        private static string GetKnownInstallPath(string runtimeName, bool useChocoAlt)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return runtimeName switch
            {
                "bun" => Path.Combine(localAppData, ".bun", "bin", "bun.exe"),
                "uv" => Path.Combine(localAppData, "uv", "uv.exe"),
                "winget" => Path.Combine(localAppData, "Microsoft", "WindowsApps", "winget.exe"),
                "scoop" => Path.Combine(userProfile, "scoop", "shims", "scoop.cmd"),
                "choco" => useChocoAlt
                    ? @"C:\ProgramData\chocolatey\bin\choco.exe"
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe"),
                _ => runtimeName
            };
        }
    }
}
