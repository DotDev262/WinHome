using System;
using Microsoft.Win32;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides concrete registry directory tree entry lookup keys, parsing qualified system string 
    /// layout structures into root hive instances managed through abstraction interfaces.
    /// </summary>
    public class RegistryWrapper : IRegistryWrapper
    {
        /// <summary>
        /// Splits a fully qualified system file path string into structural hive names and subkey directory arrays, 
        /// resolving the matching root node wrapper handle securely.
        /// </summary>
        /// <param name="fullPath">The absolute registry path sequence entry containing root hives and sub-paths (e.g., <c>"HKCU\Software\Git"</c>).</param>
        /// <param name="subKey">When this method returns, contains the parsed subkey pathway string segment trailing past the root hive boundary node.</param>
        /// <returns>An encapsulated <see cref="IRegistryKey"/> wrapper representing the resolved root destination branch path.</returns>
        /// <exception cref="ArgumentException">Thrown when the initial directory prefix array segment does not match a recognizable system hive acronym signature.</exception>
        public IRegistryKey GetRootKey(string fullPath, out string subKey)
        {
            // Partition the absolute target location into [0] Hive Name and [1] Internal Subdirectory Path strings
            string[] parts = fullPath.Split('\\', 2);
            subKey = parts.Length > 1 ? parts[1] : string.Empty;

            var rootKey = parts[0].ToUpper() switch
            {
                "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
                "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                _ => throw new ArgumentException($"Unknown Hive: {parts[0]}")
            };

            return new RegistryKeyWrapper(rootKey);
        }
    }
}