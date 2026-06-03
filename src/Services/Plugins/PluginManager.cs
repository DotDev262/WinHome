using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models.Plugins;
using WinHome.Services.Bootstrappers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinHome.Services.Plugins
{
  /// <summary>Discovers plugins from the plugins directory and ensures their runtimes are available.</summary>
  public class PluginManager : IPluginManager
  {
    private readonly UvBootstrapper _uvBootstrapper;
    private readonly BunBootstrapper _bunBootstrapper;
    private readonly ILogger _logger;
    private readonly string _pluginsDir;
    private readonly IRuntimeResolver? _runtimeResolver;

    /// <summary>Initializes a new instance of <see cref="PluginManager"/>.</summary>
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

    /// <summary>Scans the plugins directory for plugin.yaml manifests and returns them.</summary>
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

    /// <summary>Ensures the runtime required by the plugin type (Python/uv, TypeScript/bun, PowerShell) is installed.</summary>
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
