using Microsoft.Win32;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class RegistryWrapper : IRegistryWrapper
    {
        public IRegistryKey GetRootKey(string fullPath, out string subKey)
        {
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
