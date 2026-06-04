using WinHome.Models.Plugins;

namespace WinHome.Interfaces
{
  /// <summary>Discovers and manages plugins from the configured plugin directories.</summary>
  public interface IPluginManager
  {
    /// <summary>Enumerates all discovered plugin manifests.</summary>
    IEnumerable<PluginManifest> DiscoverPlugins();
    /// <summary>Ensures the required runtime is installed for the given plugin.</summary>
    Task EnsureRuntimeAsync(PluginManifest plugin);
  }
}
