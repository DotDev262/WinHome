using WinHome.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for reading and applying system-level settings.
    /// </summary>
    public interface ISystemSettingsService
    {
        /// <summary>
        /// Returns the registry tweaks corresponding to the provided settings dictionary.
        /// </summary>
        /// <param name="settings">A dictionary of setting keys and values.</param>
        /// <returns>A task resolving to an enumerable of <see cref="RegistryTweak"/> objects.</returns>
        Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object>? settings);

        /// <summary>
        /// Applies non-registry system settings from the provided dictionary.
        /// </summary>
        /// <param name="settings">A dictionary of setting keys and values.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ApplyNonRegistrySettingsAsync(Dictionary<string, object>? settings, bool dryRun);

        /// <summary>
        /// Captures the current values of all tracked system settings.
        /// </summary>
        /// <returns>A task resolving to a dictionary of setting keys and their current values.</returns>
        Task<Dictionary<string, object>> GetCapturedSettingsAsync();

        /// <summary>
        /// Returns a human-readable name for the specified registry path and value name.
        /// </summary>
        /// <param name="registryPath">The registry key path.</param>
        /// <param name="registryName">The registry value name.</param>
        /// <returns>A friendly display name, or <c>null</c> if none is defined.</returns>
        string? GetFriendlyName(string registryPath, string registryName);
    }
}