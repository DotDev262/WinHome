using Microsoft.Win32;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for resolving a root registry key from a full registry path.
    /// </summary>
    public interface IRegistryWrapper
    {
        /// <summary>
        /// Resolves the root <see cref="IRegistryKey"/> from a full registry path.
        /// </summary>
        /// <param name="fullPath">The full registry path including the hive prefix.</param>
        /// <param name="subKey">The remaining subkey path after the hive is stripped.</param>
        /// <returns>The root <see cref="IRegistryKey"/> corresponding to the hive.</returns>
        IRegistryKey GetRootKey(string fullPath, out string subKey);
    }
}