using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
              if (pwshResolved != "pwsh")
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

    /// <summary>Downloads and installs missing plugins from the remote repository archive.</summary>
    public async Task EnsurePluginsInstalledAsync(IEnumerable<string> configuredPluginNames)
    {
      if (!Directory.Exists(_pluginsDir))
      {
        Directory.CreateDirectory(_pluginsDir);
      }

      var missingPlugins = configuredPluginNames.Where(name =>
      {
        var manifestPath = Path.Combine(_pluginsDir, name, "plugin.yaml");
        return !File.Exists(manifestPath);
      }).ToList();

      if (!missingPlugins.Any())
      {
        return;
      }

      _logger.LogInfo($"[PluginManager] Missing local plugins: {string.Join(", ", missingPlugins)}. Downloading fresh plugin pack from GitHub...");

      try
      {
        using var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WinHome-CLI");

        var zipUrl = "https://github.com/DotDev262/WinHome/archive/refs/heads/main.zip";
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"winhome-plugins-{Guid.NewGuid()}.zip");

        using (var response = await client.GetAsync(zipUrl))
        {
          response.EnsureSuccessStatusCode();
          using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
          {
            await response.Content.CopyToAsync(fs);
          }
        }

        var tempExtractPath = Path.Combine(Path.GetTempPath(), $"winhome-extract-{Guid.NewGuid()}");
        System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

        var extractedPluginsDir = Path.Combine(tempExtractPath, "WinHome-main", "plugins");
        if (Directory.Exists(extractedPluginsDir))
        {
          foreach (var dir in Directory.GetDirectories(extractedPluginsDir))
          {
            var pluginName = Path.GetFileName(dir);
            var targetDir = Path.Combine(_pluginsDir, pluginName);
            if (Directory.Exists(targetDir))
            {
              Directory.Delete(targetDir, true);
            }
            CopyDirectory(dir, targetDir);
          }
          _logger.LogSuccess("[PluginManager] Plugin pack downloaded and extracted successfully.");
        }
        else
        {
          _logger.LogError("[PluginManager] Failed to locate plugins folder in downloaded archive.");
        }

        try
        {
          File.Delete(tempZipPath);
          Directory.Delete(tempExtractPath, true);
        }
        catch { /* ignored */ }
      }
      catch (Exception ex)
      {
        _logger.LogError($"[PluginManager] Failed to download plugin pack: {ex.Message}");
      }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
      Directory.CreateDirectory(destinationDir);
      foreach (var file in Directory.GetFiles(sourceDir))
      {
        File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);
      }
      foreach (var subDir in Directory.GetDirectories(sourceDir))
      {
        CopyDirectory(subDir, Path.Combine(destinationDir, Path.GetFileName(subDir)));
      }
    }
  }
}
