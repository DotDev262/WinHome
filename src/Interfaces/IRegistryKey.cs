using Microsoft.Win32;

namespace WinHome.Interfaces
{
    public interface IRegistryKey : IDisposable
    {
        void SetValue(string name, object value, RegistryValueKind kind);
        object? GetValue(string name);
        void DeleteValue(string name);
        IRegistryKey? OpenSubKey(string name, bool writable);
        IRegistryKey CreateSubKey(string name, bool writable);
    }
}
