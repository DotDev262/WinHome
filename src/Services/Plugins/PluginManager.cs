using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models.Plugins;
using WinHome.Services.Bootstrappers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Services.Plugins
{
    public class PluginManager : IPluginManager
    {
        private readonly UvBootstrapper _uvBootstrapper;
        private readonly BunBootstrapper _bunBootstrapper;
        private readonly ILogger _logger;
        private readonly string _pluginsDir;

        public PluginManager(
            UvBootstrapper uvBootstrapper,
            BunBootstrapper bunBootstrapper,
            ILogger logger,
            string? pluginsDirectory = null)
        {
            _uvBootstrapper = uvBootstrapper;
            _bunBootstrapper = bunBootstrapper;
            _logger = logger;

            _pluginsDir = pluginsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinHome",
                "plugins");
        }

        public IEnumerable<PluginManifest> DiscoverPlugins()
        {
            if (!Directory.Exists(_pluginsDir))
            {
                return Enumerable.Empty<PluginManifest>();
            }

            var plugins = new List<PluginManifest>();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (var dir in Directory.GetDirectories(_pluginsDir))
            {
                var manifestPath = Path.Combine(dir, "plugin.yaml");
                if (File.Exists(manifestPath))
                {
                    try
                    {
                        var content = File.ReadAllText(manifestPath);
                        var manifest = deserializer.Deserialize<PluginManifest>(content);
                        manifest.DirectoryPath = dir;
                        plugins.Add(manifest);
                        _logger.LogInfo($"[Plugin] Discovered: {manifest.Name} ({manifest.Type})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Plugin] Failed to load manifest in {dir}: {ex.Message}");
                    }
                }
            }

            return plugins;
        }

        public async Task EnsureRuntimeAsync(PluginManifest plugin)
        {
            switch (plugin.Type.ToLower())
            {
                case "python":
                    if (!_uvBootstrapper.IsInstalled())
                    {
                        _logger.LogInfo($"[Plugin] {plugin.Name} requires 'uv'. Installing...");
                        await Task.Run(() => _uvBootstrapper.Install(false));
                    }
                    break;

                case "typescript":
                case "javascript":
                    if (!_bunBootstrapper.IsInstalled())
                    {
                        _logger.LogInfo($"[Plugin] {plugin.Name} requires 'bun'. Installing...");
                        await Task.Run(() => _bunBootstrapper.Install(false));
                    }
                    break;
            }
        }
    }
}
