using Microsoft.Win32;
using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    [SupportedOSPlatform("windows")]
    public class RegistryService: IRegistryService
    {
        public void Apply(RegistryTweak tweak, bool dryRun)
        {
            try
            {
                RegistryKey root = GetRootKey(tweak.Path, out string subKeyPath);

                
                using (RegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
                {
                    object? currentValue = key?.GetValue(tweak.Name);
                    
                    if (currentValue != null && currentValue.ToString() == tweak.Value.ToString())
                    {
                        Console.WriteLine($"[Registry] Skipped: {tweak.Name} (Already set)");
                        return;
                    }

                    if (dryRun)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[DryRun] Would set Registry: {tweak.Path}\\{tweak.Name} = {tweak.Value}");
                        Console.ResetColor();
                        return;
                    }
                }

                
                using (RegistryKey key = root.CreateSubKey(subKeyPath, writable: true))
                {
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

        public void Revert(string path, string name, bool dryRun)
        {
             try
            {
                RegistryKey root = GetRootKey(path, out string subKeyPath);
                using (RegistryKey? key = root.OpenSubKey(subKeyPath, writable: !dryRun))
                {
                    if (key == null) return;

                    if (key.GetValue(name) != null)
                    {
                        if (dryRun)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[DryRun] Would delete Registry value: {path}\\{name}");
                            Console.ResetColor();
                            return;
                        }

                        key.DeleteValue(name);
                        Console.WriteLine($"[Registry] Reverted {name}");
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