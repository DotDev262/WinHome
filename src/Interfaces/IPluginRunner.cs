using WinHome.Models.Plugins;

namespace WinHome.Interfaces
{
    public interface IPluginRunner
    {
        Task<PluginResult> ExecuteAsync(PluginManifest plugin, string command, object? args, object? context);
    }
}
