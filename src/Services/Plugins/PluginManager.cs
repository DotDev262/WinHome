using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models.Plugins;
using WinHome.Services.Bootstrappers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Services.Plugins
{
    /// <summary>
    /// Manages the registration, file system discovery, configuration loading, 
    /// and runtime dependency validation for system extendability plugins.
    /// </summary>
    public class PluginManager : IPluginManager
    {
        private readonly UvBootstrapper _uvBootstrapper;
        private readonly BunBootstrapper _bunBootstrapper;
        private readonly ILogger _logger;
        private readonly string _pluginsDir;
        private readonly IRuntimeResolver? _runtimeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager"/> class with required environment bootstrappers and log tools.
        /// </summary>
        /// <param name="uvBootstrapper">The Python package management tool setup utility instance.</param>
        /// <param name="bunBootstrapper">The JavaScript/TypeScript fast engine setup tool utility instance.</param>
        /// <param name="logger">The logging pipeline configuration instance used to output status updates.</param>
        /// <param name="pluginsDirectory">The custom target directory path string override for locating extensions, or null to default to local app data.</param>
        /// <param name="runtimeResolver">The environment execution path verification and mapping configuration utility lookup system.</param>
        public PluginManager(
            UvBootstrapper uvBootstrapper,
            BunBootstrapper bunBootstrapper,
            ILogger logger,
            string? pluginsDirectory = null,
            IRuntimeResolver? runtimeResolver = null)
        {
            _uvBootstrapper = uvBootstrapper;
            _bunBootstrapper = bunBootstrapper;
            _logger = logger;
            _runtimeResolver = runtimeResolver;

            _pluginsDir = pluginsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinHome",
                "plugins");
        }

        /// <summary>
        /// Traverses the system plugins storage directory looking for subfolders containing valid YAML descriptor documents to load.
        /// </summary>
        /// <returns>A collection of read and verified configuration structural metadata instances representing found extension entries.</returns>
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Plugin] Failed to load manifest in {dir}: {ex.Message}");
                    }
                }
            }

            return plugins;
        }

        /// <summary>
        /// Evaluates a plugin's targeting platform configuration settings and triggers automated installation of missing external script runtimes.
        /// </summary>
        /// <param name="plugin">The structural context metadata properties model representing the target plugin requiring execution host validation.</param>
        /// <returns>A asynchronous processing placeholder <see cref="Task"/> context wrapping structural verification steps.</returns>
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

                case "powershell":
                    string resolvedMessage = "Assuming system powershell is available.";
                    if (_runtimeResolver != null)
                    {
                        try
                        {
                            var pwshResolved = _runtimeResolver.Resolve("pwsh");
                            if (pwshResolved != "pwsh" && File.Exists(pwshResolved))
                            {
                                resolvedMessage = "Using pwsh (Core).";
                            }
                            else
                            {
                                resolvedMessage = "Falling back to Windows PowerShell.";
                            }
                        }
                        catch
                        {
                            resolvedMessage = "Falling back to Windows PowerShell.";
                        }
                    }
                    _logger.LogInfo($"[Plugin] {plugin.Name} requires 'powershell'. {resolvedMessage}");
                    break;
            }
        }
    }
}