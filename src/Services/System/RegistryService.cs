using Microsoft.Win32;
using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    [SupportedOSPlatform("windows")]
    public class RegistryService : IRegistryService
    {
        private readonly IRegistryWrapper _registryWrapper;

        public RegistryService(IRegistryWrapper registryWrapper)
        {
            _registryWrapper = registryWrapper;
        }

        public void Apply(RegistryTweak tweak, bool dryRun)
        {
            try
            {
                IRegistryKey root = _registryWrapper.GetRootKey(tweak.Path, out string subKeyPath);

                using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
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

                using (IRegistryKey key = root.CreateSubKey(subKeyPath, writable: true))
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
                IRegistryKey root = _registryWrapper.GetRootKey(path, out string subKeyPath);
                using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: !dryRun))
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

        public object? Read(string path, string name)
        {
            try
            {
                IRegistryKey root = _registryWrapper.GetRootKey(path, out string subKeyPath);
                using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
                {
                    return key?.GetValue(name);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}