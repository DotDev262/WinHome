using Microsoft.Win32;

namespace WinHome.Interfaces
{
  /// <summary>Parses a full registry path into a root hive key and subkey path.</summary>
  public interface IRegistryWrapper
  {
    /// <summary>Resolves a full registry path (e.g. "HKEY_CURRENT_USER\Software\Foo") into a root key and subkey path.</summary>
    /// <param name="fullPath">The full registry path.</param>
    /// <param name="subKey">Outputs the subkey path relative to the root.</param>
    /// <returns>An <see cref="IRegistryKey"/> for the root hive.</returns>
    IRegistryKey GetRootKey(string fullPath, out string subKey);
  }
}
