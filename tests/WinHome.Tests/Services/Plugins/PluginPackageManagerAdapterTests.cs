using System.Text.Json;
using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Models.Plugins;
using WinHome.Services.Plugins;

namespace WinHome.Tests.Services.Plugins
{
    public class PluginPackageManagerAdapterTests
    {
        private readonly PluginManifest _plugin = new()
        {
            Name = "custom-plugin",
            Type = "python",
            Main = "main.py",
            DirectoryPath = Path.GetTempPath()
        };

        [Fact]
        public void Constructor_Throws_WhenPluginManifestIsMissing()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();

            Assert.Throws<ArgumentNullException>(() =>
                new PluginPackageManagerAdapter(null!, runner.Object, manager.Object, resolver.Object));
        }

        [Fact]
        public void Install_EnsuresRuntime_AndDelegatesToPluginRunner()
        {
            var runner = new Mock<IPluginRunner>(MockBehavior.Strict);
            var manager = new Mock<IPluginManager>(MockBehavior.Strict);
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);
            var app = new AppConfig { Id = "git", Version = "2.48.1", Params = "--global" };
            var sequence = new MockSequence();

            manager.InSequence(sequence)
                .Setup(m => m.EnsureRuntimeAsync(_plugin))
                .Returns(Task.CompletedTask);
            runner.InSequence(sequence)
                .Setup(r => r.ExecuteAsync(
                    _plugin,
                    "install",
                    It.Is<object>(args => MatchesInstallArgs(args, "git", "2.48.1", "--global")),
                    It.Is<object>(context => MatchesDryRunContext(context, true)),
                    null))
                .ReturnsAsync(new PluginResult { Success = true });

            adapter.Install(app, true);

            manager.Verify(m => m.EnsureRuntimeAsync(_plugin), Times.Once);
            runner.VerifyAll();
        }

        [Fact]
        public void Uninstall_EnsuresRuntime_AndDelegatesToPluginRunner()
        {
            var runner = new Mock<IPluginRunner>(MockBehavior.Strict);
            var manager = new Mock<IPluginManager>(MockBehavior.Strict);
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);
            var sequence = new MockSequence();

            manager.InSequence(sequence)
                .Setup(m => m.EnsureRuntimeAsync(_plugin))
                .Returns(Task.CompletedTask);
            runner.InSequence(sequence)
                .Setup(r => r.ExecuteAsync(
                    _plugin,
                    "uninstall",
                    It.Is<object>(args => MatchesPackageIdArgs(args, "git")),
                    It.Is<object>(context => MatchesDryRunContext(context, false)),
                    null))
                .ReturnsAsync(new PluginResult { Success = true });

            adapter.Uninstall("git", false);

            manager.Verify(m => m.EnsureRuntimeAsync(_plugin), Times.Once);
            runner.VerifyAll();
        }

        [Fact]
        public void IsAvailable_ReturnsFalse_WhenRequiredRuntimeCannotBeResolved()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            resolver.Setup(r => r.Resolve("uv")).Returns(string.Empty);
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            var available = adapter.IsAvailable();

            Assert.False(available);
        }

        [Fact]
        public void IsAvailable_ReturnsTrue_WhenRequiredRuntimeExists()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            resolver.Setup(r => r.Resolve("uv")).Returns("dotnet");
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            var available = adapter.IsAvailable();

            Assert.True(available);
        }

        [Fact]
        public void IsInstalled_ReturnsTrue_WhenPluginReportsInstalled()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            runner.Setup(r => r.ExecuteAsync(
                    _plugin,
                    "check_installed",
                    It.Is<object>(args => MatchesPackageIdArgs(args, "git")),
                    null,
                    null))
                .ReturnsAsync(new PluginResult { Success = true, Data = true });

            var installed = adapter.IsInstalled("git");

            Assert.True(installed);
        }

        [Fact]
        public void IsInstalled_ReturnsFalse_WhenPluginResponseCannotBeParsed()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            runner.Setup(r => r.ExecuteAsync(
                    _plugin,
                    "check_installed",
                    It.IsAny<object>(),
                    null,
                    null))
                .ReturnsAsync(new PluginResult { Success = true, Data = "definitely" });

            var installed = adapter.IsInstalled("git");

            Assert.False(installed);
        }

        [Fact]
        public void Install_Throws_WhenPluginReportsFailure()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            manager.Setup(m => m.EnsureRuntimeAsync(_plugin)).Returns(Task.CompletedTask);
            runner.Setup(r => r.ExecuteAsync(_plugin, "install", It.IsAny<object>(), It.IsAny<object>(), null))
                .ReturnsAsync(new PluginResult { Success = false, Error = "Plugin exploded." });

            var ex = Assert.Throws<Exception>(() => adapter.Install(new AppConfig { Id = "git" }, false));

            Assert.Equal("Plugin 'custom-plugin' failed to install 'git': Plugin exploded.", ex.Message);
        }

        [Fact]
        public void Uninstall_Throws_WhenPluginReportsFailure()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            manager.Setup(m => m.EnsureRuntimeAsync(_plugin)).Returns(Task.CompletedTask);
            runner.Setup(r => r.ExecuteAsync(_plugin, "uninstall", It.IsAny<object>(), It.IsAny<object>(), null))
                .ReturnsAsync(new PluginResult { Success = false, Error = "Plugin exploded." });

            var ex = Assert.Throws<Exception>(() => adapter.Uninstall("git", false));

            Assert.Equal("Plugin 'custom-plugin' failed to uninstall 'git': Plugin exploded.", ex.Message);
        }

        [Fact]
        public void Install_ThrowsTimeoutException_WhenRunnerTimesOut()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            manager.Setup(m => m.EnsureRuntimeAsync(_plugin)).Returns(Task.CompletedTask);
            runner.Setup(r => r.ExecuteAsync(_plugin, "install", It.IsAny<object>(), It.IsAny<object>(), null))
                .Returns(Task.FromException<PluginResult>(new TimeoutException("Plugin timed out after 30s.")));

            var ex = Assert.Throws<TimeoutException>(() => adapter.Install(new AppConfig { Id = "git" }, false));

            Assert.Equal("Plugin 'custom-plugin' timed out while executing 'install'.", ex.Message);
        }

        [Fact]
        public void Install_ThrowsInvalidOperationException_WhenRunnerReturnsNullResponse()
        {
            var runner = new Mock<IPluginRunner>();
            var manager = new Mock<IPluginManager>();
            var resolver = new Mock<IRuntimeResolver>();
            var adapter = new PluginPackageManagerAdapter(_plugin, runner.Object, manager.Object, resolver.Object);

            manager.Setup(m => m.EnsureRuntimeAsync(_plugin)).Returns(Task.CompletedTask);
            runner.Setup(r => r.ExecuteAsync(_plugin, "install", It.IsAny<object>(), It.IsAny<object>(), null))
                .ReturnsAsync((PluginResult)null!);

            var ex = Assert.Throws<InvalidOperationException>(() => adapter.Install(new AppConfig { Id = "git" }, false));

            Assert.Equal("Plugin 'custom-plugin' returned an invalid response for 'install'.", ex.Message);
        }

        private static bool MatchesInstallArgs(object? args, string packageId, string? version, string? parameters)
        {
            if (args is null)
            {
                return false;
            }

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(args));
            var root = doc.RootElement;
            return HasString(root, "packageId", packageId)
                && HasNullableString(root, "version", version)
                && HasNullableString(root, "params", parameters);
        }

        private static bool MatchesPackageIdArgs(object? args, string packageId)
        {
            if (args is null)
            {
                return false;
            }

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(args));
            return HasString(doc.RootElement, "packageId", packageId);
        }

        private static bool MatchesDryRunContext(object? context, bool dryRun)
        {
            if (context is null)
            {
                return false;
            }

            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(context));
            return doc.RootElement.TryGetProperty("dryRun", out var value)
                && value.ValueKind == (dryRun ? JsonValueKind.True : JsonValueKind.False);
        }

        private static bool HasString(JsonElement root, string propertyName, string expectedValue)
        {
            return root.TryGetProperty(propertyName, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() == expectedValue;
        }

        private static bool HasNullableString(JsonElement root, string propertyName, string? expectedValue)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                return false;
            }

            if (expectedValue is null)
            {
                return value.ValueKind == JsonValueKind.Null;
            }

            return value.ValueKind == JsonValueKind.String && value.GetString() == expectedValue;
        }
    }
}
