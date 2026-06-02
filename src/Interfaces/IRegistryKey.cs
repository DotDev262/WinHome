using Microsoft.Win32;

namespace WinHome.Interfaces
{
  /// <summary>Abstraction wrapping a Windows Registry key for testability.</summary>
  public interface IRegistryKey : IDisposable
  {
    /// <summary>Sets the specified registry value.</summary>
    void SetValue(string name, object value, RegistryValueKind kind);
    /// <summary>Gets the value of a registry entry, or <c>null</c> if not found.</summary>
    object? GetValue(string name);
    /// <summary>Deletes the specified registry value.</summary>
    void DeleteValue(string name);
    /// <summary>Opens a subkey with the specified write access.</summary>
    IRegistryKey? OpenSubKey(string name, bool writable);
    /// <summary>Creates or opens a subkey with the specified write access.</summary>
    IRegistryKey? CreateSubKey(string name, bool writable);
  }
}
