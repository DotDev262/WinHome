using Moq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinHome.Interfaces;
using WinHome.Models.Plugins;
using WinHome.Services.Plugins;
using Xunit;

namespace WinHome.Tests.Services.Plugins
{
    public class PluginRunnerTests : IDisposable
    {
        private readonly string _tempDir;

        public PluginRunnerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "WinHomePluginRunnerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }

        private PluginRunner CreateRunner(Mock<ILogger> mockLogger)
        {
            var mockResolver = new Mock<IRuntimeResolver>();
            return new PluginRunner(mockLogger.Object, mockResolver.Object);
        }

        private PluginManifest CreateCrossPlatformManifest(string name, string windowsScript, string unixScript)
        {
            string ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            string fileName = name + ext;
            string fullPath = Path.Combine(_tempDir, fileName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.WriteAllText(fullPath, "@echo off\n" + windowsScript);
            }
            else
            {
                File.WriteAllText(fullPath, "#!/bin/sh\n" + unixScript.Replace("\r\n", "\n"));
                try { File.SetUnixFileMode(fullPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); } catch { }
            }

            return new PluginManifest
            {
                Name = name,
                Type = "executable",
                Main = fileName,
                DirectoryPath = _tempDir
            };
        }

        [Fact]
        public async Task ExecuteAsync_CompletesWithinTimeout_ReturnsNormalResponse()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var runner = CreateRunner(mockLogger);

            var manifest = CreateCrossPlatformManifest("test-fast",
                "set /p dummy=\necho {\"success\": true, \"changed\": false, \"data\": null}",
                "read dummy\necho '{\"success\": true, \"changed\": false, \"data\": null}'");

            // Act
            var result = await runner.ExecuteAsync(manifest, "test", null, null, TimeSpan.FromSeconds(5));

            // Assert
            Assert.True(result.Success, result.Error);
        }

        [Fact]
        public async Task ExecuteAsync_ExceedsTimeout_KillsProcessAndReturnsError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var runner = CreateRunner(mockLogger);

            // Windows ping to localhost is a common way to sleep in cmd without external tools (timeout command can be flaky in CI)
            // Or just use PowerShell for sleep inside the cmd script since PowerShell is ubiquitous on Windows
            var manifest = CreateCrossPlatformManifest("test-slow",
                "powershell -NoProfile -Command \"Start-Sleep -Seconds 5\"\necho {\"success\": true}",
                "sleep 5\necho '{\"success\": true}'");

            var sw = Stopwatch.StartNew();

            // Act - set a very short timeout (1 second)
            var result = await runner.ExecuteAsync(manifest, "test", null, null, TimeSpan.FromSeconds(1));
            sw.Stop();

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Plugin timed out after 1s.", result.Error);
            Assert.True(sw.ElapsedMilliseconds < 10000, $"Process should have been killed quickly, but took {sw.ElapsedMilliseconds}ms");

            // Verify a warning was logged containing the duration
            mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("timed out and was killed after"))), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_TimeoutWithStderr_StderrIsLogged()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var runner = CreateRunner(mockLogger);

            var manifest = CreateCrossPlatformManifest("test-stderr-slow",
                "powershell -NoProfile -Command \"[Console]::Error.WriteLine('Warning: Doing some work before sleeping'); Start-Sleep -Seconds 5\"",
                "echo 'Warning: Doing some work before sleeping' >&2\nsleep 5");

            // Act
            var result = await runner.ExecuteAsync(manifest, "test", null, null, TimeSpan.FromSeconds(2));

            // Assert
            Assert.False(result.Success);
            Assert.Contains("timed out", result.Error);

            // Since stderr is read asynchronously, it should be logged as a warning
            mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("[STDERR]") && s.Contains("Warning: Doing some work before sleeping"))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ZeroOrNegativeTimeout_DefaultsTo1SecondMinimum()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var runner = CreateRunner(mockLogger);

            var manifest = CreateCrossPlatformManifest("test-fast-zero",
                "set /p dummy=\necho {\"success\": true, \"changed\": false, \"data\": null}",
                "read dummy\necho '{\"success\": true, \"changed\": false, \"data\": null}'");

            // Act - set timeout to Zero
            var result = await runner.ExecuteAsync(manifest, "test", null, null, TimeSpan.Zero);

            // Assert - The process should succeed because the minimum clamped timeout (1s) is enough for this fast script
            Assert.True(result.Success, result.Error);
        }
    }
}
