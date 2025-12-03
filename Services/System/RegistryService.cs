using Microsoft.Win32;
using System.Runtime.Versioning;
using WinHome.Models;

namespace WinHome.Services.System
{
    [SupportedOSPlatform("windows")]
    public class RegistryService
    {
        public void Apply(RegistryTweak tweak)
        {
            try
            {
                RegistryKey root = GetRootKey(tweak.Path, out string subKeyPath);

                using (RegistryKey key = root.CreateSubKey(subKeyPath, writable: true))
                {
                    object? currentValue = key.GetValue(tweak.Name);
                    
                    if (currentValue != null && currentValue.ToString() == tweak.Value.ToString())
                    {
                        Console.WriteLine($"[Registry] Skipped: {tweak.Name} (Already set)");
                        return;
                    }

                    RegistryValueKind kind = tweak.Type.ToLower() switch
                    {
                        "dword" => RegistryValueKind.DWord,
                        "qword" => RegistryValueKind.QWord,
                        "binary" => RegistryValueKind.Binary,
                        _ => RegistryValueKind.String
                    };

                    object valueToWrite = tweak.Value;
                    if (kind == RegistryValueKind.DWord) valueToWrite = Convert.ToInt32(tweak.Value);
                    if (kind == RegistryValueKind.QWord) valueToWrite = Convert.ToInt64(tweak.Value);

                    key.SetValue(tweak.Name, valueToWrite, kind);
                    Console.WriteLine($"[Registry] Set {tweak.Name} = {tweak.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Registry apply failed: {ex.Message}");
            }
        }

        // NEW: Revert method to delete the value (Restore Default)
        public void Revert(string path, string name)
        {
            try
            {
                RegistryKey root = GetRootKey(path, out string subKeyPath);
                
                // Open existing key (don't create if missing)
                using (RegistryKey? key = root.OpenSubKey(subKeyPath, writable: true))
                {
                    if (key == null) return; // Key doesn't exist, nothing to revert

                    if (key.GetValue(name) != null)
                    {
                        key.DeleteValue(name);
                        Console.WriteLine($"[Registry] Reverted {name} (Deleted value)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Registry revert failed: {ex.Message}");
            }
        }

        private RegistryKey GetRootKey(string fullPath, out string subKey)
        {
            string[] parts = fullPath.Split('\\', 2);
            subKey = parts.Length > 1 ? parts[1] : string.Empty;

            return parts[0].ToUpper() switch
            {
                "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
                "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                _ => throw new ArgumentException($"Unknown Hive: {parts[0]}")
            };
        }
    }
}