using Moq;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models.Plugins;
using WinHome.Services.Plugins;
using WinHome.Services.Bootstrappers;
using Xunit;

namespace WinHome.Tests
{
    public class PluginIntegrationTests
    {
        [Fact]
        public async Task PluginRunner_RealExecution_TalksToPythonViaUv()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var resolver = new WinHome.Services.System.RuntimeResolver(mockLogger.Object);

            // Skip if uv is not installed
            try { 
                resolver.Resolve("uv"); 
            } catch { 
                return; // Skip test if runtime not found
            }

            var runner = new PluginRunner(mockLogger.Object, resolver);
            
            // We need to point to the actual test plugin we just created
            var projectRoot = Directory.GetCurrentDirectory();
            // Move up from bin/Debug/net10.0-windows to tests/WinHome.Tests
            var current = new DirectoryInfo(projectRoot);
            while (current != null && current.Name != "tests") 
            {
                current = current.Parent;
            }
            
            var pluginPath = Path.GetFullPath(Path.Combine(current!.FullName, "TestPlugin"));

            var manifest = new PluginManifest
            {
                Name = "test-echo",
                Type = "python",
                Main = "src/main.py",
                DirectoryPath = pluginPath
            };

            var args = new { message = "Hello from C#" };
            var context = new { dryRun = false };

            // Act
            var result = await runner.ExecuteAsync(manifest, "echo", args, context);

            // Assert
            Assert.True(result.Success, $"Execution failed: {result.Error}");
            Assert.True(result.Changed);
            
            var dataJson = (JsonElement)result.Data!;
            var echoMessage = dataJson.GetProperty("echo").GetString();
            
            Assert.Equal("Hello from C#", echoMessage);
            
            // Verify Stderr wasn't used for errors (though we might have logged info)
            // mockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PluginRunner_RealExecution_TalksToJsViaBun()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var resolver = new WinHome.Services.System.RuntimeResolver(mockLogger.Object);

            // Skip if bun is not installed
            try { 
                resolver.Resolve("bun"); 
            } catch { 
                return; // Skip test if runtime not found
            }

            var runner = new PluginRunner(mockLogger.Object, resolver);
            
            // We need to point to the actual test plugin we just created
            var projectRoot = Directory.GetCurrentDirectory();
            // Move up from bin/Debug/net10.0-windows to tests/WinHome.Tests
            var current = new DirectoryInfo(projectRoot);
            while (current != null && current.Name != "tests") 
            {
                current = current.Parent;
            }
            
            var pluginPath = Path.GetFullPath(Path.Combine(current!.FullName, "TestPluginJS"));

            var manifest = new PluginManifest
            {
                Name = "test-echo-js",
                Type = "javascript",
                Main = "src/index.js",
                DirectoryPath = pluginPath
            };

            var args = new { message = "Hello from Bun" };
            var context = new { dryRun = false };

            // Act
            var result = await runner.ExecuteAsync(manifest, "echo", args, context);

            // Assert
            Assert.True(result.Success, $"Execution failed: {result.Error}");
            
            var dataJson = (JsonElement)result.Data!;
            var echoMessage = dataJson.GetProperty("echo").GetString();
            var runtime = dataJson.GetProperty("runtime").GetString();
            
            Assert.Equal("Hello from Bun", echoMessage);
            Assert.Equal("bun", runtime);
        }
    }
}
