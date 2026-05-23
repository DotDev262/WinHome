using Moq;
using System.Diagnostics;
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
            // Just use powershell for Windows testing
            mockResolver.Setup(r => r.Resolve(It.IsAny<string>())).Returns("powershell");
            return new PluginRunner(mockLogger.Object, mockResolver.Object);
        }

        [Fact]
        public async Task ExecuteAsync_CompletesWithinTimeout_ReturnsNormalResponse()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var runner = CreateRunner(mockLogger);

            string ps1Path = Path.Combine(_tempDir, "fast.ps1");
            File.WriteAllText(ps1Path, @"
Write-Output '{""success"": true, ""changed"": false, ""data"": null}'
");

            var manifest = new PluginManifest
            {
                Name = "test-fast",
                Type = "powershell",
                Main = "fast.ps1",
                DirectoryPath = _tempDir
            };

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

            string ps1Path = Path.Combine(_tempDir, "slow.ps1");
            File.WriteAllText(ps1Path, @"
Start-Sleep -Seconds 5
Write-Output '{""success"": true, ""changed"": false, ""data"": null}'
");

            var manifest = new PluginManifest
            {
                Name = "test-slow",
                Type = "powershell",
                Main = "slow.ps1",
                DirectoryPath = _tempDir
            };

            var sw = Stopwatch.StartNew();

            // Act - set a very short timeout (1 second)
            var result = await runner.ExecuteAsync(manifest, "test", null, null, TimeSpan.FromSeconds(1));
            sw.Stop();

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Plugin timed out after 1 seconds", result.Error);
            Assert.True(sw.ElapsedMilliseconds < 4000, $"Process should have been killed quickly, but took {sw.ElapsedMilliseconds}ms");

            // Verify a warning was logged containing the duration
            mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("timed out and was killed after"))), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_TimeoutWithStderr_StderrIsLogged()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var runner = CreateRunner(mockLogger);

            string ps1Path = Path.Combine(_tempDir, "stderr-slow.ps1");
            // The script writes to stderr then sleeps, causing a timeout
            File.WriteAllText(ps1Path, @"
[Console]::Error.WriteLine('Warning: Doing some work before sleeping')
Start-Sleep -Seconds 5
");

            var manifest = new PluginManifest
            {
                Name = "test-stderr-slow",
                Type = "powershell",
                Main = "stderr-slow.ps1",
                DirectoryPath = _tempDir
            };

            // Act
            var result = await runner.ExecuteAsync(manifest, "test", null, null, TimeSpan.FromSeconds(2));

            // Assert
            Assert.False(result.Success);
            Assert.Contains("timed out", result.Error);

            // Since stderr is read asynchronously, it should be logged as a warning
            // PowerShell might output some extra text, so we check for the text we injected
            mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("[STDERR]") && s.Contains("Warning: Doing some work before sleeping"))), Times.AtLeastOnce);
        }
    }
}
