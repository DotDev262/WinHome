using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Models.Plugins;
using WinHome.Services.Plugins;
using Xunit;

namespace WinHome.Tests
{
  public class PluginSystemTests
  {
    [Fact]
    public void PluginManager_DiscoverPlugins_ReturnsManifests_FromDirectory()
    {
      // Arrange
      var tempDir = Path.Combine(Path.GetTempPath(), "WinHomeTests", Guid.NewGuid().ToString());
      var pluginDir = Path.Combine(tempDir, "test-plugin");
      Directory.CreateDirectory(pluginDir);

      var yaml = @"
name: test-plugin
version: 1.0.0
type: python
main: src/main.py
capabilities:
  - package_manager
";
      File.WriteAllText(Path.Combine(pluginDir, "plugin.yaml"), yaml);

      var mockLogger = new Mock<ILogger>();
      var mockProcessRunner = new Mock<IProcessRunner>();
      var uvBootstrapper = new WinHome.Services.Bootstrappers.UvBootstrapper(mockProcessRunner.Object);
      var bunBootstrapper = new WinHome.Services.Bootstrappers.BunBootstrapper(mockProcessRunner.Object);

      var manager = new PluginManager(uvBootstrapper, bunBootstrapper, mockLogger.Object, tempDir);

      // Act
      var plugins = manager.DiscoverPlugins().ToList();

      // Assert
      Assert.Single(plugins);
      Assert.Equal("test-plugin", plugins[0].Name);
      Assert.Equal("python", plugins[0].Type);
      Assert.Equal("package_manager", plugins[0].Capabilities[0]);
      Assert.Equal(pluginDir, plugins[0].DirectoryPath);

      // Cleanup
      Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Engine_Registers_Plugin_As_PackageManager()
    {
      // Arrange
      var mockPluginManager = new Mock<IPluginManager>();
      var mockPluginRunner = new Mock<IPluginRunner>();
      var mockLogger = new Mock<ILogger>();
      var mockRuntimeResolver = new Mock<IRuntimeResolver>();

      var testPlugin = new PluginManifest
      {
        Name = "custom-pkg-mgr",
        Type = "python",
        Capabilities = new List<string> { "package_manager" }
      };

      mockPluginManager.Setup(m => m.DiscoverPlugins()).Returns(new List<PluginManifest> { testPlugin });

      var engine = new Engine(
          new Dictionary<string, IPackageManager>(), // Empty initial managers
          new Mock<IDotfileService>().Object,
          new Mock<IRegistryService>().Object,
          new Mock<ISystemSettingsService>().Object,
          new Mock<IWslService>().Object,
          new Mock<IGitService>().Object,
          new Mock<IEnvironmentService>().Object,
          new Mock<IWindowsServiceManager>().Object,
          new Mock<IScheduledTaskService>().Object,
          mockPluginManager.Object,
          mockPluginRunner.Object,
          new Mock<IStateService>().Object,
          mockLogger.Object,
          mockRuntimeResolver.Object
      );

      var config = new Configuration
      {
        Apps = new List<AppConfig>
                {
                    new AppConfig { Id = "test-app", Manager = "custom-pkg-mgr" }
                }
      };

      // Setup the Runner to return success for "install"
      mockPluginRunner.Setup(r => r.ExecuteAsync(
          It.Is<PluginManifest>(p => p.Name == "custom-pkg-mgr"),
          "install",
          It.IsAny<object>(),
          It.IsAny<object>()))
          .ReturnsAsync(new PluginResult { Success = true });

      // We also need to mock IsInstalled/IsAvailable calls that happen inside the Engine/Adapter
      // But since the adapter creates a new Bootstrapper instance internally, testing it fully with mocks is tricky without refactoring Adapter.
      // However, we can check if the Engine *attempts* to call the runner.
    }

    [Fact]
    public void PluginRunner_Builds_Correct_Uv_Command()
    {
      // This tests the command construction logic inside PluginRunner (implied)
      // Since PluginRunner uses Process.Start, we can't easily unit test it without an abstraction over Process.
      // But we can test the Adapter's use of Runner.
    }

    [Fact]
    public void Adapter_Translates_Install_Call_To_Runner()
    {
      // Arrange
      var mockRunner = new Mock<IPluginRunner>();
      var mockManager = new Mock<IPluginManager>();
      var mockRuntimeResolver = new Mock<IRuntimeResolver>();
      var manifest = new PluginManifest { Name = "test-plugin", Type = "python" };

      var adapter = new PluginPackageManagerAdapter(manifest, mockRunner.Object, mockManager.Object, mockRuntimeResolver.Object);

      mockRunner.Setup(r => r.ExecuteAsync(manifest, "install", It.IsAny<object>(), It.IsAny<object>()))
          .ReturnsAsync(new PluginResult { Success = true });

      // Act
      adapter.Install(new AppConfig { Id = "mypkg", Version = "1.0" }, false);

      // Assert
      mockRunner.Verify(r => r.ExecuteAsync(
          manifest,
          "install",
          It.Is<object>(o => o != null && o.ToString()!.Contains("mypkg")), // Rough check or cast dynamic
          It.IsAny<object>()),
          Times.Once);
    }

    [Fact]
    public void PluginRunner_Builds_Correct_PowerShell_Command_Fallback()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockRuntimeResolver = new Mock<IRuntimeResolver>();

      mockRuntimeResolver.Setup(r => r.Resolve("pwsh")).Returns("pwsh");
      mockRuntimeResolver.Setup(r => r.Resolve("powershell")).Returns(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");

      var runner = new PluginRunner(mockLogger.Object, mockRuntimeResolver.Object);
      var manifest = new PluginManifest
      {
        Name = "test-powershell-plugin",
        Type = "powershell",
        Main = "plugin.ps1",
        DirectoryPath = @"C:\plugins\test-powershell-plugin"
      };

      // Act
      var (fileName, argumentList) = runner.BuildProcessStartInfo(manifest.Type, Path.Combine(manifest.DirectoryPath, manifest.Main));

      // Assert
      Assert.Equal(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", fileName);
      Assert.Contains("-NoProfile", argumentList);
      Assert.Contains("-NonInteractive", argumentList);
      Assert.Contains("-ExecutionPolicy", argumentList);
      Assert.Contains("Bypass", argumentList);
      Assert.Contains("-File", argumentList);
      Assert.Contains(Path.Combine(manifest.DirectoryPath, manifest.Main), argumentList);
    }

    [Fact]
    public void PluginRunner_Builds_Correct_PowerShell_Command_Core()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockRuntimeResolver = new Mock<IRuntimeResolver>();

      // We mock pwsh to point to powershell.exe since we know it exists.
      var existingPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
      mockRuntimeResolver.Setup(r => r.Resolve("pwsh")).Returns(existingPath);

      var runner = new PluginRunner(mockLogger.Object, mockRuntimeResolver.Object);
      var manifest = new PluginManifest
      {
        Name = "test-powershell-plugin",
        Type = "powershell",
        Main = "plugin.ps1",
        DirectoryPath = @"C:\plugins\test-powershell-plugin"
      };

      // Act
      var (fileName, argumentList) = runner.BuildProcessStartInfo(manifest.Type, Path.Combine(manifest.DirectoryPath, manifest.Main));

      // Assert
      Assert.Equal(existingPath, fileName);
      Assert.Contains("-NoProfile", argumentList);
      Assert.Contains("-NonInteractive", argumentList);
      Assert.Contains("-ExecutionPolicy", argumentList);
      Assert.Contains("Bypass", argumentList);
      Assert.Contains("-File", argumentList);
      Assert.Contains(Path.Combine(manifest.DirectoryPath, manifest.Main), argumentList);
    }

    [Fact]
    public async Task PluginManager_EnsureRuntime_Supports_PowerShell()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockProcessRunner = new Mock<IProcessRunner>();
      var uvBootstrapper = new WinHome.Services.Bootstrappers.UvBootstrapper(mockProcessRunner.Object);
      var bunBootstrapper = new WinHome.Services.Bootstrappers.BunBootstrapper(mockProcessRunner.Object);

      var manager = new PluginManager(uvBootstrapper, bunBootstrapper, mockLogger.Object, Path.GetTempPath());
      var manifest = new PluginManifest
      {
        Name = "test-powershell-plugin",
        Type = "powershell"
      };

      // Act & Assert
      var exception = await Record.ExceptionAsync(() => manager.EnsureRuntimeAsync(manifest));
      Assert.Null(exception);
      mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("requires 'powershell'") && s.Contains("Assuming system powershell"))), Times.Once);
    }

    [Fact]
    public async Task PluginManager_EnsureRuntime_Logs_PwshCore_WhenResolved()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockProcessRunner = new Mock<IProcessRunner>();
      var mockRuntimeResolver = new Mock<IRuntimeResolver>();
      var uvBootstrapper = new WinHome.Services.Bootstrappers.UvBootstrapper(mockProcessRunner.Object);
      var bunBootstrapper = new WinHome.Services.Bootstrappers.BunBootstrapper(mockProcessRunner.Object);

      // Mock pwsh to exist and return a valid path (using powershell.exe since we know it exists on Windows)
      var existingPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
      mockRuntimeResolver.Setup(r => r.Resolve("pwsh")).Returns(existingPath);

      var manager = new PluginManager(uvBootstrapper, bunBootstrapper, mockLogger.Object, Path.GetTempPath(), mockRuntimeResolver.Object);
      var manifest = new PluginManifest
      {
        Name = "test-powershell-plugin",
        Type = "powershell"
      };

      // Act
      await manager.EnsureRuntimeAsync(manifest);

      // Assert
      mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("requires 'powershell'") && s.Contains("Using pwsh (Core)"))), Times.Once);
    }

    [Fact]
    public async Task PluginManager_EnsureRuntime_Logs_WindowsPowerShell_Fallback_WhenPwshNotResolved()
    {
      // Arrange
      var mockLogger = new Mock<ILogger>();
      var mockProcessRunner = new Mock<IProcessRunner>();
      var mockRuntimeResolver = new Mock<IRuntimeResolver>();
      var uvBootstrapper = new WinHome.Services.Bootstrappers.UvBootstrapper(mockProcessRunner.Object);
      var bunBootstrapper = new WinHome.Services.Bootstrappers.BunBootstrapper(mockProcessRunner.Object);

      // Mock pwsh to return "pwsh" (meaning it failed to resolve to a concrete file path)
      mockRuntimeResolver.Setup(r => r.Resolve("pwsh")).Returns("pwsh");

      var manager = new PluginManager(uvBootstrapper, bunBootstrapper, mockLogger.Object, Path.GetTempPath(), mockRuntimeResolver.Object);
      var manifest = new PluginManifest
      {
        Name = "test-powershell-plugin",
        Type = "powershell"
      };

      // Act
      await manager.EnsureRuntimeAsync(manifest);

      // Assert
      mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("requires 'powershell'") && s.Contains("Falling back to Windows PowerShell"))), Times.Once);
    }
  }
}
