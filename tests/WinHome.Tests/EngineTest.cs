using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Models.Plugins;
using Xunit;

namespace WinHome.Tests
{
  public class EngineTests
  {
    private readonly Mock<IPackageManager> _mockWinget;
    private readonly Mock<IDotfileService> _mockDotfiles;
    private readonly Mock<IRegistryService> _mockRegistry;
    private readonly Mock<ISystemSettingsService> _mockSystemSettings;
    private readonly Mock<IWslService> _mockWsl;
    private readonly Mock<IGitService> _mockGit;
    private readonly Mock<IEnvironmentService> _mockEnv;
    private readonly Mock<IWindowsServiceManager> _mockServiceManager;
    private readonly Mock<IScheduledTaskService> _mockScheduledTaskService;
    private readonly Mock<IPluginManager> _mockPluginManager;
    private readonly Mock<IPluginRunner> _mockPluginRunner;
    private readonly Mock<IStateService> _mockStateService;
    private readonly Mock<IRuntimeResolver> _mockRuntimeResolver;
    private readonly Dictionary<string, IPackageManager> _managers;

    public EngineTests()
    {
      // 1. Create Mocks
      _mockWinget = new Mock<IPackageManager>();
      _mockDotfiles = new Mock<IDotfileService>();
      _mockRegistry = new Mock<IRegistryService>();
      _mockSystemSettings = new Mock<ISystemSettingsService>();
      _mockWsl = new Mock<IWslService>();
      _mockGit = new Mock<IGitService>();
      _mockEnv = new Mock<IEnvironmentService>();
      _mockServiceManager = new Mock<IWindowsServiceManager>();
      _mockScheduledTaskService = new Mock<IScheduledTaskService>();
      _mockPluginManager = new Mock<IPluginManager>();
      _mockPluginRunner = new Mock<IPluginRunner>();
      _mockStateService = new Mock<IStateService>();
      _mockRuntimeResolver = new Mock<IRuntimeResolver>();
      var mockLogger = new Mock<ILogger>();

      // Setup basic behavior
      _mockWinget.Setup(x => x.IsAvailable()).Returns(true);
      _mockRegistry.Setup(r => r.Apply(It.IsAny<RegistryTweak>(), It.IsAny<bool>())).Returns(true);
      _mockRegistry.Setup(r => r.Revert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
      _mockSystemSettings.Setup(x => x.GetTweaksAsync(It.IsAny<Dictionary<string, object>>()))
                         .Returns(Task.FromResult<IEnumerable<RegistryTweak>>(new List<RegistryTweak>()));
      _mockPluginManager.Setup(m => m.DiscoverPlugins()).Returns(new List<PluginManifest>());
      _mockStateService.Setup(s => s.LoadState()).Returns(new StateData());
      _mockSystemSettings
          .Setup(s => s.ApplyNonRegistrySettingsAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<bool>()))
          .Returns(Task.CompletedTask);
      _mockSystemSettings
          .Setup(s => s.CaptureOriginalSettingsAsync(It.IsAny<Dictionary<string, object>>()))
          .ReturnsAsync(new Dictionary<string, object>());

      // 2. Setup Manager Dictionary
      _managers = new Dictionary<string, IPackageManager>
            {
                { "winget", _mockWinget.Object }
            };
    }

    [Fact]
    public async Task RunAsync_ShouldInstallApps_WhenConfigured()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "TestApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      // dryRun = false
      await engine.RunAsync(config, false);

      // Assert
      // Verify that Install was called exactly once for "TestApp"
      _mockWinget.Verify(x => x.Install(
          It.Is<AppConfig>(a => a.Id == "TestApp"),
          false),
          Times.Once);
      _mockStateService.Verify(s => s.MarkAsApplied(It.IsAny<string>()), Times.Never);
      _mockStateService.Verify(s => s.SaveState(It.Is<StateData>(state => state.AppliedItems.SetEquals(new[] { "winget:TestApp" }))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_DryRun_ShouldPassFlagToService()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "DryRunApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      // dryRun = TRUE
      await engine.RunAsync(config, true);

      // Assert
      // Verify that Install was called with dryRun = true
      _mockWinget.Verify(x => x.Install(
          It.Is<AppConfig>(a => a.Id == "DryRunApp"),
          true),
          Times.Once);
    }

    [Fact]
    public async Task PrintDiffAsync_ShouldPrintCorrectDiff()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "UnchangedApp", Manager = "winget" });
      config.Apps.Add(new AppConfig { Id = "NewApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      var previousState = new StateData();
      previousState.AppliedItems.Add("winget:UnchangedApp");
      previousState.AppliedItems.Add("winget:OldApp");
      _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

      // Act
      await engine.PrintDiffAsync(config);

      // Assert
      mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Items to Remove"))), Times.Once);
      mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("App (winget): OldApp"))), Times.Once);
      mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("Items to Add"))), Times.Once);
      mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("App (winget): NewApp"))), Times.Once);
      mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("Unchanged Items"))), Times.Once);
      mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("App (winget): UnchangedApp"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldApplyDotfiles_WhenConfigured()
    {
      // Arrange
      var config = new Configuration();
      config.Dotfiles.Add(new DotfileConfig { Src = "C:\\src\\dotfile", Target = "C:\\target\\dotfile" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockDotfiles.Verify(d => d.Apply(It.Is<DotfileConfig>(df => df.Src == "C:\\src\\dotfile" && df.Target == "C:\\target\\dotfile"), false), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldApplyRegistryTweaksAndSystemSettings_WhenConfigured()
    {
      // Arrange
      var config = new Configuration();
      config.RegistryTweaks.Add(new RegistryTweak
      {
        Path = "HKCU\\Software\\WinHome",
        Name = "SettingA",
        Value = 1,
        Type = "dword"
      });
      config.SystemSettings["explorer.showHiddenFiles"] = true;

      var presetTweak = new RegistryTweak
      {
        Path = "HKCU\\Software\\Preset",
        Name = "PresetSetting",
        Value = 1,
        Type = "dword"
      };

      _mockSystemSettings
          .Setup(s => s.GetTweaksAsync(It.IsAny<Dictionary<string, object>>()))
          .ReturnsAsync(new List<RegistryTweak> { presetTweak });

      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockRegistry.Verify(r => r.Apply(It.Is<RegistryTweak>(t => t.Path == "HKCU\\Software\\WinHome" && t.Name == "SettingA"), false), Times.Once);
      _mockRegistry.Verify(r => r.Apply(It.Is<RegistryTweak>(t => t.Path == "HKCU\\Software\\Preset" && t.Name == "PresetSetting"), false), Times.Once);
      _mockSystemSettings.Verify(s => s.ApplyNonRegistrySettingsAsync(It.Is<Dictionary<string, object>>(d => d.ContainsKey("explorer.showHiddenFiles")), false), Times.Once);
      _mockStateService.Verify(s => s.MarkAsApplied(It.IsAny<string>()), Times.Never);
      _mockStateService.Verify(s => s.SaveState(It.Is<StateData>(state => state.AppliedItems.SetEquals(new[] { "reg:HKCU\\Software\\WinHome|SettingA", "reg:HKCU\\Software\\Preset|PresetSetting" }))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldConfigureWslAndGit_WhenConfigured()
    {
      // Arrange
      var config = new Configuration
      {
        Wsl = new WslConfig { DefaultVersion = 2, Update = false },
        Git = new GitConfig { UserName = "Test User", UserEmail = "user@example.com" }
      };
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockWsl.Verify(w => w.Configure(It.Is<WslConfig>(c => c.DefaultVersion == 2), false), Times.Once);
      _mockGit.Verify(g => g.Configure(It.Is<GitConfig>(c => c.UserName == "Test User"), false), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldApplyEnvironmentVariables_WhenConfigured()
    {
      // Arrange
      var config = new Configuration();
      config.EnvVars.Add(new EnvVarConfig { Variable = "TEST_ENV", Value = "VALUE", Action = "set" });
      config.EnvVars.Add(new EnvVarConfig { Variable = "PATH", Value = "C:\\Tools", Action = "append" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "TEST_ENV" && v.Value == "VALUE"), false), Times.Once);
      _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "PATH" && v.Value == "C:\\Tools"), false), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldApplyProfileEnvironmentOverrides_WhenProfileSelected()
    {
      // Arrange
      var config = new Configuration();
      config.EnvVars.Add(new EnvVarConfig { Variable = "EDITOR", Value = "nvim", Action = "set" });
      config.EnvVars.Add(new EnvVarConfig { Variable = "Path", Value = "C:\\GlobalTools", Action = "append" });
      config.Profiles["work"] = new ProfileConfig
      {
        EnvVars = new List<EnvVarConfig>
                {
                    new EnvVarConfig { Variable = "EDITOR", Value = "code", Action = "set" },
                    new EnvVarConfig { Variable = "Path", Value = "C:\\WorkTools", Action = "append" }
                }
      };

      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false, "work");

      // Assert
      _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "EDITOR" && v.Value == "nvim"), false), Times.Never);
      _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "EDITOR" && v.Value == "code"), false), Times.Once);
      _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "Path" && v.Value == "C:\\GlobalTools"), false), Times.Once);
      _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "Path" && v.Value == "C:\\WorkTools"), false), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldManageServicesAndScheduledTasks_WhenConfigured()
    {
      // Arrange
      var config = new Configuration();
      config.Services.Add(new WindowsServiceConfig { Name = "TestService", State = "running", StartupType = "automatic" });
      config.ScheduledTasks.Add(new ScheduledTaskConfig { Name = "TestTask", Path = "\\\\WinHome\\\\Test", Description = "Task" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockServiceManager.Verify(s => s.Apply(It.Is<WindowsServiceConfig>(svc => svc.Name == "TestService" && svc.State == "running"), false), Times.Once);
      _mockScheduledTaskService.Verify(t => t.Apply(It.Is<ScheduledTaskConfig>(task => task.Name == "TestTask" && task.Path == "\\\\WinHome\\\\Test"), false), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldExecutePluginExtensions_WhenConfigured()
    {
      // Arrange
      var config = new Configuration();
      config.Extensions["demo"] = new Dictionary<string, object> { { "setting", "value" } };

      var plugin = new PluginManifest
      {
        Name = "demo",
        Type = "executable",
        Main = "demo.exe",
        Capabilities = new List<string>()
      };

      _mockPluginManager.Setup(m => m.DiscoverPlugins()).Returns(new List<PluginManifest> { plugin });
      _mockPluginRunner
          .Setup(r => r.ExecuteAsync(plugin, "apply", It.IsAny<object>(), It.IsAny<object>()))
          .ReturnsAsync(new PluginResult { Success = true, Changed = true });

      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockPluginManager.Verify(m => m.EnsureRuntimeAsync(plugin), Times.Once);
      _mockPluginRunner.Verify(r => r.ExecuteAsync(plugin, "apply", It.IsAny<object>(), It.Is<object>(o => o != null)), Times.Once);
    }

    [Fact]
    public async Task PrintDiffAsync_ShouldIncludeAllSectionTypes()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "NewApp", Manager = "winget" });
      config.RegistryTweaks.Add(new RegistryTweak
      {
        Path = "HKCU\\Software\\Custom",
        Name = "CustomSetting",
        Value = 1,
        Type = "dword"
      });
      config.SystemSettings["system.preset"] = true;

      var presetTweak = new RegistryTweak
      {
        Path = "HKCU\\Software\\Preset",
        Name = "PresetSetting",
        Value = 1,
        Type = "dword"
      };

      _mockSystemSettings
          .Setup(s => s.GetTweaksAsync(It.IsAny<Dictionary<string, object>>()))
          .ReturnsAsync(new List<RegistryTweak> { presetTweak });
      _mockSystemSettings
          .Setup(s => s.GetFriendlyName("HKCU\\Software\\Preset", "PresetSetting"))
          .Returns("system.preset");

      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      _mockStateService.Setup(s => s.LoadState()).Returns(new StateData());

      // Act
      await engine.PrintDiffAsync(config);

      // Assert
      mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("App (winget): NewApp"))), Times.Once);
      mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("Registry Tweak: HKCU\\Software\\Custom -> CustomSetting"))), Times.Once);
      mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("System Setting: system.preset"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldRemoveStateOnlyAfterSuccessfulCleanup()
    {
      // Arrange
      var config = new Configuration();

      var previousState = new StateData();
      previousState.AppliedItems.Add("winget:OldApp");
      previousState.AppliedItems.Add("reg:HKCU\\Software\\Old|OldSetting");

      _mockStateService.Setup(s => s.LoadState()).Returns(previousState);
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockWinget.Verify(m => m.Uninstall("OldApp", false), Times.Once);
      _mockRegistry.Verify(r => r.Revert("HKCU\\Software\\Old", "OldSetting", false), Times.Once);
      _mockStateService.Verify(s => s.RemoveApplied("winget:OldApp"), Times.Once);
      _mockStateService.Verify(s => s.RemoveApplied("reg:HKCU\\Software\\Old|OldSetting"), Times.Once);
      _mockStateService.Verify(s => s.SaveState(It.IsAny<StateData>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldNotPersistUnknownManagerAsApplied()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "MissingApp", Manager = "unknown-manager" });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockStateService.Verify(s => s.MarkAsApplied("unknown-manager:MissingApp"), Times.Never);
      _mockStateService.Verify(s => s.SaveState(It.IsAny<StateData>()), Times.Never);
      mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Unknown manager: unknown-manager"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldNotMarkFailedRegistryApplyAsApplied()
    {
      // Arrange
      var config = new Configuration();
      config.RegistryTweaks.Add(new RegistryTweak
      {
        Path = "HKCU\\Software\\WinHome",
        Name = "FailedSetting",
        Value = 1,
        Type = "dword"
      });
      _mockRegistry.Setup(r => r.Apply(It.IsAny<RegistryTweak>(), false)).Returns(false);
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false, continueOnError: true);

      // Assert
      _mockStateService.Verify(s => s.MarkAsApplied("reg:HKCU\\Software\\WinHome|FailedSetting"), Times.Never);
      _mockStateService.Verify(s => s.SaveState(It.IsAny<StateData>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldPreserveTrackedSystemSettingOriginals()
    {
      // Arrange
      var config = new Configuration();
      config.SystemSettings["brightness"] = 80;
      _mockSystemSettings
          .Setup(s => s.CaptureOriginalSettingsAsync(It.IsAny<Dictionary<string, object>>()))
          .ReturnsAsync(new Dictionary<string, object> { ["brightness"] = 50 });
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockStateService.Verify(s => s.TrackSystemSettingOriginal("brightness", 50), Times.Once);
      _mockStateService.Verify(s => s.SaveState(It.IsAny<StateData>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldNotCaptureOriginals_WhenDryRun()
    {
      // Arrange
      var config = new Configuration();
      config.SystemSettings["brightness"] = 80;
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, true);

      // Assert
      _mockSystemSettings.Verify(s => s.CaptureOriginalSettingsAsync(It.IsAny<Dictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldPersistCapturedSystemSettingOriginals()
    {
      if (!OperatingSystem.IsWindows()) return;

      // Arrange
      var config = new Configuration();
      config.SystemSettings["brightness"] = 80;

      _mockSystemSettings
          .Setup(s => s.CaptureOriginalSettingsAsync(It.IsAny<Dictionary<string, object>>()))
          .ReturnsAsync(new Dictionary<string, object> { ["brightness"] = 45 });

      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockStateService.Verify(s => s.TrackSystemSettingOriginal("brightness", 45), Times.Once);
      _mockStateService.Verify(s => s.SaveState(It.IsAny<StateData>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldNotOverwriteExistingSystemSettingOriginals()
    {
      if (!OperatingSystem.IsWindows()) return;

      // Arrange
      var config = new Configuration();
      config.SystemSettings["brightness"] = 80;

      var previousState = new StateData();
      previousState.SystemSettingOriginals["brightness"] = 45;
      _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

      _mockSystemSettings
          .Setup(s => s.CaptureOriginalSettingsAsync(It.IsAny<Dictionary<string, object>>()))
          .ReturnsAsync(new Dictionary<string, object> { ["brightness"] = 80 });

      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false);

      // Assert
      _mockStateService.Verify(s => s.TrackSystemSettingOriginal("brightness", It.IsAny<object>()), Times.Never);
      _mockStateService.Verify(s => s.SaveState(It.IsAny<StateData>()), Times.Never);
    }

    [Fact]
    public async Task PrintDiffAsync_ShouldShowSystemSettingsReverts()
    {
      // Arrange
      var previousState = new StateData();
      previousState.SystemSettingOriginals["brightness"] = 45;
      _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

      var config = new Configuration();
      var mockLogger = new Mock<ILogger>();
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.PrintDiffAsync(config);

      // Assert
      mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("System Settings to Revert"))), Times.Once);
      mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("brightness"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldThrow_WhenStepFails_AndContinueOnErrorIsFalse()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "FailApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      _mockWinget.Setup(x => x.Install(It.IsAny<AppConfig>(), It.IsAny<bool>())).Throws(new Exception("Install failed"));
      var engine = CreateEngine(mockLogger);

      // Act & Assert
      await Assert.ThrowsAsync<Exception>(() => engine.RunAsync(config, false, continueOnError: false));
    }

    [Fact]
    public async Task RunAsync_ShouldNotThrow_WhenStepFails_AndContinueOnErrorIsTrue()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "FailApp", Manager = "winget" });
      config.Apps.Add(new AppConfig { Id = "NextApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      _mockWinget.Setup(x => x.Install(It.Is<AppConfig>(a => a.Id == "FailApp"), It.IsAny<bool>())).Throws(new Exception("Install failed"));
      var engine = CreateEngine(mockLogger);

      // Act
      await engine.RunAsync(config, false, continueOnError: true);

      // Assert
      _mockWinget.Verify(x => x.Install(It.Is<AppConfig>(a => a.Id == "NextApp"), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipPreviouslyAppliedSteps()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "SkipApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
      try
      {
        var stateWriter = new WinHome.Services.StateWriter(tmp);
        stateWriter.RecordStep(new StepResult
        {
          StepId = "winget:SkipApp",
          StepType = "app",
          StepName = "SkipApp",
          Status = StepStatus.Succeeded,
          AppliedAt = DateTime.UtcNow
        });

        var engine = new Engine(
            _managers, _mockDotfiles.Object, _mockRegistry.Object, _mockSystemSettings.Object,
            _mockWsl.Object, _mockGit.Object, _mockEnv.Object, _mockServiceManager.Object,
            _mockScheduledTaskService.Object, _mockPluginManager.Object, _mockPluginRunner.Object,
            _mockStateService.Object, mockLogger.Object, _mockRuntimeResolver.Object, stateWriter);

        // Act
        await engine.RunAsync(config, false, forceReapply: false);

        // Assert
        _mockWinget.Verify(x => x.Install(It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Never);
      }
      finally
      {
        if (File.Exists(tmp)) File.Delete(tmp);
      }
    }

    [Fact]
    public async Task RunAsync_ShouldForceReapply_WhenFlagIsSet()
    {
      // Arrange
      var config = new Configuration();
      config.Apps.Add(new AppConfig { Id = "ReapplyApp", Manager = "winget" });
      var mockLogger = new Mock<ILogger>();
      var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
      try
      {
        var stateWriter = new WinHome.Services.StateWriter(tmp);
        stateWriter.RecordStep(new StepResult
        {
          StepId = "winget:ReapplyApp",
          StepType = "app",
          StepName = "ReapplyApp",
          Status = StepStatus.Succeeded,
          AppliedAt = DateTime.UtcNow
        });

        var engine = new Engine(
            _managers, _mockDotfiles.Object, _mockRegistry.Object, _mockSystemSettings.Object,
            _mockWsl.Object, _mockGit.Object, _mockEnv.Object, _mockServiceManager.Object,
            _mockScheduledTaskService.Object, _mockPluginManager.Object, _mockPluginRunner.Object,
            _mockStateService.Object, mockLogger.Object, _mockRuntimeResolver.Object, stateWriter);

        // Act
        await engine.RunAsync(config, false, forceReapply: true);

        // Assert
        _mockWinget.Verify(x => x.Install(It.IsAny<AppConfig>(), It.IsAny<bool>()), Times.Once);
      }
      finally
      {
        if (File.Exists(tmp)) File.Delete(tmp);
        if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
      }
    }

        private Engine CreateEngine(Mock<ILogger> logger)
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
            var stateWriter = new WinHome.Services.StateWriter(tmp);
            return new Engine(
                _managers,
                _mockDotfiles.Object,
                _mockRegistry.Object,
                _mockSystemSettings.Object,
                _mockWsl.Object,
                _mockGit.Object,
                _mockEnv.Object,
                _mockServiceManager.Object,
                _mockScheduledTaskService.Object,
                _mockPluginManager.Object,
                _mockPluginRunner.Object,
                _mockStateService.Object,
                logger.Object,
                _mockRuntimeResolver.Object,
                stateWriter
            );
        }

        [Fact]
        public async Task RunAsync_Cleanup_RemovesStateWriterEntry_And_AllowsReapply_ForApp()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
            try
            {
                var stateWriter = new WinHome.Services.StateWriter(tmp);
                stateWriter.RecordStep(new StepResult
                {
                    StepId = "winget:ReaddApp",
                    StepType = "app",
                    StepName = "ReaddApp",
                    Status = StepStatus.Succeeded,
                    AppliedAt = DateTime.UtcNow
                });

                var previousState = new StateData();
                previousState.AppliedItems.Add("winget:ReaddApp");
                _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

                var mockLogger = new Mock<ILogger>();
                var engine = new Engine(
                    _managers, _mockDotfiles.Object, _mockRegistry.Object, _mockSystemSettings.Object,
                    _mockWsl.Object, _mockGit.Object, _mockEnv.Object, _mockServiceManager.Object,
                    _mockScheduledTaskService.Object, _mockPluginManager.Object, _mockPluginRunner.Object,
                    _mockStateService.Object, mockLogger.Object, _mockRuntimeResolver.Object, stateWriter);

                // Act: cleanup run
                await engine.RunAsync(new Configuration(), false);

                // Assert cleanup removed applied state and removed StateWriter entry
                _mockWinget.Verify(m => m.Uninstall("ReaddApp", false), Times.Once);
                _mockStateService.Verify(s => s.RemoveApplied("winget:ReaddApp"), Times.Once);
                var loadedAfterCleanup = stateWriter.Load();
                Assert.False(loadedAfterCleanup.ContainsKey("winget:ReaddApp"));

                // Act: re-add and apply
                _mockStateService.Setup(s => s.LoadState()).Returns(new StateData());
                var config = new Configuration();
                config.Apps.Add(new AppConfig { Id = "ReaddApp", Manager = "winget" });
                await engine.RunAsync(config, false);

                // Verify re-apply
                _mockWinget.Verify(m => m.Install(It.Is<AppConfig>(a => a.Id == "ReaddApp"), false), Times.Once);
                var loadedAfterReapply = stateWriter.Load();
                Assert.True(loadedAfterReapply.ContainsKey("winget:ReaddApp"));
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
            }
        }

        [Fact]
        public async Task RunAsync_Cleanup_RemovesStateWriterEntry_And_AllowsReapply_ForRegistry()
        {
            if (!OperatingSystem.IsWindows()) return;

            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
            try
            {
                var stateWriter = new WinHome.Services.StateWriter(tmp);
                var stepId = "reg:HKCU\\Software\\Readd|ReaddSetting";
                stateWriter.RecordStep(new StepResult
                {
                    StepId = stepId,
                    StepType = "registry",
                    StepName = "ReaddSetting",
                    Status = StepStatus.Succeeded,
                    AppliedAt = DateTime.UtcNow
                });

                var previousState = new StateData();
                previousState.AppliedItems.Add(stepId);
                _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

                var mockLogger = new Mock<ILogger>();
                var engine = new Engine(
                    _managers, _mockDotfiles.Object, _mockRegistry.Object, _mockSystemSettings.Object,
                    _mockWsl.Object, _mockGit.Object, _mockEnv.Object, _mockServiceManager.Object,
                    _mockScheduledTaskService.Object, _mockPluginManager.Object, _mockPluginRunner.Object,
                    _mockStateService.Object, mockLogger.Object, _mockRuntimeResolver.Object, stateWriter);

                // Act: cleanup run
                await engine.RunAsync(new Configuration(), false);

                // Assert cleanup removed applied state and removed StateWriter entry
                _mockRegistry.Verify(r => r.Revert("HKCU\\Software\\Readd", "ReaddSetting", false), Times.Once);
                _mockStateService.Verify(s => s.RemoveApplied(stepId), Times.Once);
                var loadedAfterCleanup = stateWriter.Load();
                Assert.False(loadedAfterCleanup.ContainsKey(stepId));

                // Act: re-add and apply
                _mockStateService.Setup(s => s.LoadState()).Returns(new StateData());
                var config = new Configuration();
                config.RegistryTweaks.Add(new RegistryTweak { Path = "HKCU\\Software\\Readd", Name = "ReaddSetting", Value = 1, Type = "dword" });
                await engine.RunAsync(config, false);

                // Verify re-apply
                _mockRegistry.Verify(r => r.Apply(It.Is<RegistryTweak>(t => t.Path == "HKCU\\Software\\Readd" && t.Name == "ReaddSetting"), false), Times.Once);
                var loadedAfterReapply = stateWriter.Load();
                Assert.True(loadedAfterReapply.ContainsKey(stepId));
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
            }
        }
    }
}
