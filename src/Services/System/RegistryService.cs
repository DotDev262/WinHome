using Microsoft.Win32;
using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Infrastructure.Helpers;

namespace WinHome.Services.System
{
  [SupportedOSPlatform("windows")]
  public class RegistryService : IRegistryService
  {
    private readonly IRegistryWrapper _registryWrapper;

    /// <summary>Initializes a new instance of <see cref="RegistryService"/>.</summary>
    public RegistryService(IRegistryWrapper registryWrapper)
    {
      _registryWrapper = registryWrapper;
    }

    public bool Apply(RegistryTweak tweak, bool dryRun)
    {
      try
      {
        // Security Check: Prevent HKCU modification when running as SYSTEM
        RegistryGuard.ValidateContext(tweak.Path);

        IRegistryKey root = _registryWrapper.GetRootKey(tweak.Path, out string subKeyPath);

        using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
        {
          object? currentValue = key?.GetValue(tweak.Name);

          if (currentValue != null && currentValue.ToString() == tweak.Value?.ToString())
          {
            Console.WriteLine($"[Registry] Skipped: {tweak.Name} (Already set)");
            return true;
          }

          if (dryRun)
          {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[DryRun] Would set Registry: {tweak.Path}\\{tweak.Name} = {tweak.Value}");
            Console.ResetColor();
            return true;
          }
        }

        using (IRegistryKey? key = root.CreateSubKey(subKeyPath, writable: true))
        {
          if (key == null)
          {
            Console.WriteLine($"[Error] Could not create registry subkey: {tweak.Path}");
            return false;
          }

          RegistryValueKind kind = tweak.Type.ToLower() switch
          {
            "dword" => RegistryValueKind.DWord,
            "qword" => RegistryValueKind.QWord,
            "binary" => RegistryValueKind.Binary,
            _ => RegistryValueKind.String
          };

          object? valueToWrite = tweak.Value;
          if (valueToWrite is global::System.Text.Json.JsonElement jsonElement)
          {
            if (kind == RegistryValueKind.DWord) valueToWrite = jsonElement.GetInt32();
            else if (kind == RegistryValueKind.QWord) valueToWrite = jsonElement.GetInt64();
            else valueToWrite = jsonElement.ToString() ?? string.Empty;
          }
          else
          {
            if (kind == RegistryValueKind.DWord) valueToWrite = Convert.ToInt32(tweak.Value);
            else if (kind == RegistryValueKind.QWord) valueToWrite = Convert.ToInt64(tweak.Value);
          }

          key.SetValue(tweak.Name, valueToWrite ?? string.Empty, kind);
          Console.WriteLine($"[Registry] Set {tweak.Name} = {tweak.Value}");
          return true;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Error] Registry apply failed: {ex.Message}");
        // If it's our security exception, we rethrow it or ensure it's logged as critical.
        if (ex is InvalidOperationException && ex.Message.StartsWith("Security Risk"))
        {
          throw;
        }
        return false;
      }
    }

    public bool Revert(string path, string name, bool dryRun)
    {
      try
      {
        // Security Check
        RegistryGuard.ValidateContext(path);

        IRegistryKey root = _registryWrapper.GetRootKey(path, out string subKeyPath);
        using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: !dryRun))
        {
          if (key == null) return true;

          if (key.GetValue(name) != null)
          {
            if (dryRun)
            {
              Console.ForegroundColor = ConsoleColor.Yellow;
              Console.WriteLine($"[DryRun] Would delete Registry value: {path}\\{name}");
              Console.ResetColor();
              return true;
            }

            key.DeleteValue(name);
            Console.WriteLine($"[Registry] Reverted {name}");
          }
          return true;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Error] Registry revert failed: {ex.Message}");
        if (ex is InvalidOperationException && ex.Message.StartsWith("Security Risk"))
        {
          throw;
        }
        return false;
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
