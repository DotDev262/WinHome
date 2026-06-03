using Microsoft.Win32;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
  /// <summary>Wraps a <see cref="RegistryKey"/> to implement <see cref="IRegistryKey"/> for testability.</summary>
  public class RegistryKeyWrapper : IRegistryKey
  {
    private readonly RegistryKey _registryKey;

    /// <summary>Initializes a new instance wrapping the given <see cref="RegistryKey"/>.</summary>
    public RegistryKeyWrapper(RegistryKey registryKey)
    {
      _registryKey = registryKey;
    }

    public void SetValue(string name, object value, RegistryValueKind kind)
    {
      _registryKey.SetValue(name, value, kind);
    }

    public object? GetValue(string name)
    {
      return _registryKey.GetValue(name);
    }

    public void DeleteValue(string name)
    {
      _registryKey.DeleteValue(name);
    }

    public IRegistryKey? OpenSubKey(string name, bool writable)
    {
      var subKey = _registryKey.OpenSubKey(name, writable);
      return subKey == null ? null : new RegistryKeyWrapper(subKey);
    }

    public IRegistryKey? CreateSubKey(string name, bool writable)
    {
      var subKey = _registryKey.CreateSubKey(name, writable);
      return subKey == null ? null : new RegistryKeyWrapper(subKey);
    }

    public void Dispose()
    {
      _registryKey.Dispose();
    }
  }
}
