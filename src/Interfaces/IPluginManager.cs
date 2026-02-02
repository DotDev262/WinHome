using WinHome.Models.Plugins;

namespace WinHome.Interfaces
{
    public interface IPluginManager
    {
        IEnumerable<PluginManifest> DiscoverPlugins();
        Task EnsureRuntimeAsync(PluginManifest plugin);
    }
}
