using WinHome.Models.Plugins;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for executing plugin commands.
    /// </summary>
    public interface IPluginRunner
    {
        /// <summary>
        /// Asynchronously executes a command on the specified plugin.
        /// </summary>
        /// <param name="plugin">The plugin manifest describing the plugin to run.</param>
        /// <param name="command">The command name to execute within the plugin.</param>
        /// <param name="args">Optional arguments to pass to the command.</param>
        /// <param name="context">Optional context object provided to the plugin.</param>
        /// <returns>A task that resolves to a <see cref="PluginResult"/> containing the execution result.</returns>
        Task<PluginResult> ExecuteAsync(PluginManifest plugin, string command, object? args, object? context);
    }
}