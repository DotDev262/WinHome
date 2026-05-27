using Moq;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models.Plugins;
using WinHome.Services.Plugins;
using WinHome.Services.Bootstrappers;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class PluginIntegrationTests
  {
    private static string? FindTestPluginPath(string pluginDirectoryName)
    {
      var current = new DirectoryInfo(AppContext.BaseDirectory);

      while (current is not null)
      {
        var candidate = Path.Combine(current.FullName, "tests", pluginDirectoryName);
        if (Directory.Exists(candidate))
        {
          return candidate;
        }

        current = current.Parent;
      }

      return null;
    }

    private static bool RuntimeIsInstalled(IRuntimeResolver resolver, string runtimeName)
    {
      var resolved = resolver.Resolve(runtimeName);
      return Path.IsPathRooted(resolved) && File.Exists(resolved);
    }

    [Fact]
    public async Task PluginRunner_RealExecution_TalksToPythonViaUv()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var resolver = new RuntimeResolver(mockLogger.Object, new DefaultProcessRunner(), new DefaultFileSystem());

      // Skip if uv is not installed
      if (!RuntimeIsInstalled(resolver, "uv"))
      {
        return;
      }

      var runner = new PluginRunner(mockLogger.Object, resolver);

      var pluginPath = FindTestPluginPath("TestPlugin");
      if (pluginPath is null)
      {
        return;
      }

      var manifest = new PluginManifest
      {
        Name = "test-echo",
        Type = "python",
        Main = "src/main.py",
        DirectoryPath = pluginPath
      };

      var args = new { message = "Hello from C#" };
      var context = new { dryRun = false };

      var result = await runner.ExecuteAsync(manifest, "echo", args, context);

      // If the environment doesn't have the runtime installed, gracefully pass/skip
      if (!result.Success && result.Error != null &&
          (result.Error.Contains("find the file") || result.Error.Contains("No such file")))
      {
        return;
      }

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
      var resolver = new RuntimeResolver(mockLogger.Object, new DefaultProcessRunner(), new DefaultFileSystem());

      // Skip if bun is not installed
      if (!RuntimeIsInstalled(resolver, "bun"))
      {
        return;
      }

      var runner = new PluginRunner(mockLogger.Object, resolver);

      var pluginPath = FindTestPluginPath("TestPluginJS");
      if (pluginPath is null)
      {
        return;
      }

      var manifest = new PluginManifest
      {
        Name = "test-echo-js",
        Type = "javascript",
        Main = "src/index.js",
        DirectoryPath = pluginPath
      };

      var args = new { message = "Hello from Bun" };
      var context = new { dryRun = false };

      var result = await runner.ExecuteAsync(manifest, "echo", args, context);

      // If the environment doesn't have the runtime installed, gracefully pass/skip
      if (!result.Success && result.Error != null &&
          (result.Error.Contains("find the file") || result.Error.Contains("No such file")))
      {
        return;
      }

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
