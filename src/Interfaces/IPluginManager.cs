using WinHome.Models.Plugins;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for discovering and managing WinHome plugins.
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// Discovers all available plugins in the plugins directory.
        /// </summary>
        /// <returns>A collection of <see cref="PluginManifest"/> objects describing each plugin.</returns>
        IEnumerable<PluginManifest> DiscoverPlugins();

        /// <summary>
        /// Ensures that the required runtime for the specified plugin is available.
        /// </summary>
        /// <param name="plugin">The plugin whose runtime should be ensured.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureRuntimeAsync(PluginManifest plugin);
    }
}