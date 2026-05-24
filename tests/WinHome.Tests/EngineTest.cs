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

            _mockWinget.Setup(x => x.IsAvailable()).Returns(true);
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

            _managers = new Dictionary<string, IPackageManager>
            {
                { "winget", _mockWinget.Object }
            };
        }

        private Engine CreateEngine(Mock<ILogger> logger)
        {
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
                _mockRuntimeResolver.Object
            );
        }

        [Fact]
        public async Task RunAsync_ShouldInstallApps_WhenConfigured()
        {
            var config = new Configuration();
            config.Apps.Add(new AppConfig { Id = "TestApp", Manager = "winget" });
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, false);

            _mockWinget.Verify(x => x.Install(
                It.Is<AppConfig>(a => a.Id == "TestApp"),
                false),
                Times.Once);
        }

        [Fact]
        public async Task RunAsync_DryRun_ShouldPassFlagToService()
        {
            var config = new Configuration();
            config.Apps.Add(new AppConfig { Id = "DryRunApp", Manager = "winget" });
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, true);

            _mockWinget.Verify(x => x.Install(
                It.Is<AppConfig>(a => a.Id == "DryRunApp"),
                true),
                Times.Once);
        }

        [Fact]
        public async Task PrintDiffAsync_ShouldPrintCorrectDiff()
        {
            var config = new Configuration();
            config.Apps.Add(new AppConfig { Id = "UnchangedApp", Manager = "winget" });
            config.Apps.Add(new AppConfig { Id = "NewApp", Manager = "winget" });
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            var previousState = new StateData();
            previousState.AppliedItems.Add("winget:UnchangedApp");
            previousState.AppliedItems.Add("winget:OldApp");
            _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

            await engine.PrintDiffAsync(config);

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
            var config = new Configuration();
            config.Dotfiles.Add(new DotfileConfig { Src = "C:\\src\\dotfile", Target = "C:\\target\\dotfile" });
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, false);

            _mockDotfiles.Verify(d => d.Apply(It.Is<DotfileConfig>(df => df.Src == "C:\\src\\dotfile" && df.Target == "C:\\target\\dotfile"), false), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldApplyRegistryTweaksAndSystemSettings_WhenConfigured()
        {
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

            await engine.RunAsync(config, false);

            _mockRegistry.Verify(r => r.Apply(It.Is<RegistryTweak>(t => t.Path == "HKCU\\Software\\WinHome" && t.Name == "SettingA"), false), Times.Once);
            _mockRegistry.Verify(r => r.Apply(It.Is<RegistryTweak>(t => t.Path == "HKCU\\Software\\Preset" && t.Name == "PresetSetting"), false), Times.Once);
            _mockSystemSettings.Verify(s => s.ApplyNonRegistrySettingsAsync(It.Is<Dictionary<string, object>>(d => d.ContainsKey("explorer.showHiddenFiles")), false), Times.Once);
            _mockStateService.Verify(s => s.MarkAsApplied("reg:HKCU\\Software\\WinHome|SettingA"), Times.Once);
            _mockStateService.Verify(s => s.MarkAsApplied("reg:HKCU\\Software\\Preset|PresetSetting"), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldConfigureWslAndGit_WhenConfigured()
        {
            var config = new Configuration
            {
                Wsl = new WslConfig { DefaultVersion = 2, Update = false },
                Git = new GitConfig { UserName = "Test User", UserEmail = "user@example.com" }
            };
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, false);

            _mockWsl.Verify(w => w.Configure(It.Is<WslConfig>(c => c.DefaultVersion == 2), false), Times.Once);
            _mockGit.Verify(g => g.Configure(It.Is<GitConfig>(c => c.UserName == "Test User"), false), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldApplyEnvironmentVariables_WhenConfigured()
        {
            var config = new Configuration();
            config.EnvVars.Add(new EnvVarConfig { Variable = "TEST_ENV", Value = "VALUE", Action = "set" });
            config.EnvVars.Add(new EnvVarConfig { Variable = "PATH", Value = "C:\\Tools", Action = "append" });
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, false);

            _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "TEST_ENV" && v.Value == "VALUE"), false), Times.Once);
            _mockEnv.Verify(e => e.Apply(It.Is<EnvVarConfig>(v => v.Variable == "PATH" && v.Value == "C:\\Tools"), false), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldManageServicesAndScheduledTasks_WhenConfigured()
        {
            var config = new Configuration();
            config.Services.Add(new WindowsServiceConfig { Name = "TestService", State = "running", StartupType = "automatic" });
            config.ScheduledTasks.Add(new ScheduledTaskConfig { Name = "TestTask", Path = "\\\\WinHome\\\\Test", Description = "Task" });
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, false);

            _mockServiceManager.Verify(s => s.Apply(It.Is<WindowsServiceConfig>(svc => svc.Name == "TestService" && svc.State == "running"), false), Times.Once);
            _mockScheduledTaskService.Verify(t => t.Apply(It.Is<ScheduledTaskConfig>(task => task.Name == "TestTask" && task.Path == "\\\\WinHome\\\\Test"), false), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldExecutePluginExtensions_WhenConfigured()
        {
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

            await engine.RunAsync(config, false);

            _mockPluginManager.Verify(m => m.EnsureRuntimeAsync(plugin), Times.Once);
            _mockPluginRunner.Verify(r => r.ExecuteAsync(plugin, "apply", It.IsAny<object>(), It.Is<object>(o => o != null)), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldNotCaptureOriginals_WhenDryRun()
        {
            var config = new Configuration();
            config.SystemSettings["brightness"] = 80;
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.RunAsync(config, true);

            _mockSystemSettings.Verify(s => s.CaptureOriginalSettingsAsync(It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task PrintDiffAsync_ShouldShowSystemSettingsReverts()
        {
            var previousState = new StateData();
            previousState.SystemSettingOriginals["brightness"] = 45;
            _mockStateService.Setup(s => s.LoadState()).Returns(previousState);

            var config = new Configuration();
            var mockLogger = new Mock<ILogger>();
            var engine = CreateEngine(mockLogger);

            await engine.PrintDiffAsync(config);

            mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("System Settings to Revert"))), Times.Once);
            mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("brightness"))), Times.Once);
        }
    }
}