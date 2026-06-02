using WinHome.Models.Plugins;

namespace WinHome.Interfaces
{
  /// <summary>Executes plugin commands with argument and context passing.</summary>
  public interface IPluginRunner
  {
    /// <summary>Executes the specified command on the given plugin.</summary>
    /// <param name="plugin">The plugin manifest describing the plugin to run.</param>
    /// <param name="command">The command name to execute.</param>
    /// <param name="args">Optional arguments passed to the command.</param>
    /// <param name="context">Optional context object for the execution.</param>
    /// <param name="timeout">Optional timeout; defaults to no timeout.</param>
    /// <returns>A <see cref="PluginResult"/> describing the outcome.</returns>
    Task<PluginResult> ExecuteAsync(PluginManifest plugin, string command, object? args, object? context, TimeSpan? timeout = null);
  }
}
