using Microsoft.Win32;

namespace WinHome.Interfaces
{
    public interface IRegistryWrapper
    {
        IRegistryKey GetRootKey(string fullPath, out string subKey);
    }
}
