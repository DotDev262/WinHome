using Microsoft.Win32;
using System.Linq;
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
    private readonly ILogger? _logger;

    /// <summary>Initializes a new instance of <see cref="RegistryService"/>.</summary>
    public RegistryService(IRegistryWrapper registryWrapper)
      : this(registryWrapper, null)
    {
    }

    /// <summary>Initializes a new instance of <see cref="RegistryService"/> with a logger.</summary>
    public RegistryService(IRegistryWrapper registryWrapper, ILogger? logger)
    {
      _registryWrapper = registryWrapper;
      _logger = logger;
    }

    private void LogInfo(string message)
    {
      if (_logger != null) _logger.LogInfo(message);
      else Console.WriteLine(message);
    }

    private void LogSuccess(string message)
    {
      if (_logger != null) _logger.LogSuccess(message);
      else Console.WriteLine(message);
    }

    private void LogWarning(string message)
    {
      if (_logger != null) _logger.LogWarning(message);
      else
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
      }
    }

    private void LogError(string message)
    {
      if (_logger != null) _logger.LogError(message);
      else Console.WriteLine(message);
    }

    public bool Apply(RegistryTweak tweak, bool dryRun)
    {
      if (tweak == null)
      {
        LogError("[Error] Registry tweak configuration is null.");
        return false;
      }
      if (string.IsNullOrWhiteSpace(tweak.Path))
      {
        LogError("[Error] Registry path cannot be null or empty.");
        return false;
      }
      if (string.IsNullOrWhiteSpace(tweak.Name))
      {
        LogError("[Error] Registry value name cannot be null or empty.");
        return false;
      }

      try
      {
        // Security Check: Prevent HKCU modification when running as SYSTEM
        RegistryGuard.ValidateContext(tweak.Path);

        IRegistryKey root = _registryWrapper.GetRootKey(tweak.Path, out string subKeyPath);

        using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
        {
          object? currentValue = key?.GetValue(tweak.Name);

          if (currentValue != null)
          {
            bool alreadySet = currentValue is byte[] currentBytes && tweak.Value is byte[] targetBytes
                ? currentBytes.SequenceEqual(targetBytes)
                : currentValue.ToString() == tweak.Value?.ToString();
            if (alreadySet)
            {
              LogInfo($"[Registry] Skipped: {tweak.Name} (Already set)");
              return true;
            }
          }

          if (dryRun)
          {
            LogWarning($"[DryRun] Would set Registry: {tweak.Path}\\{tweak.Name} = {tweak.Value}");
            return true;
          }
        }

        using (IRegistryKey? key = root.CreateSubKey(subKeyPath, writable: true))
        {
          if (key == null)
          {
            LogError($"[Error] Could not create registry subkey: {tweak.Path}");
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
          LogSuccess($"[Registry] Set {tweak.Name} = {tweak.Value}");
          return true;
        }
      }
      catch (Exception ex)
      {
        LogError($"[Error] Registry apply failed: {ex.Message}");
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
      if (string.IsNullOrWhiteSpace(path))
      {
        LogError("[Error] Registry path cannot be null or empty.");
        return false;
      }
      if (string.IsNullOrWhiteSpace(name))
      {
        LogError("[Error] Registry value name cannot be null or empty.");
        return false;
      }

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
              LogWarning($"[DryRun] Would delete Registry value: {path}\\{name}");
              return true;
            }

            key.DeleteValue(name);
            LogSuccess($"[Registry] Reverted {name}");
          }
          return true;
        }
      }
      catch (Exception ex)
      {
        LogError($"[Error] Registry revert failed: {ex.Message}");
        if (ex is InvalidOperationException && ex.Message.StartsWith("Security Risk"))
        {
          throw;
        }
        return false;
      }
    }

    public object? Read(string path, string name)
    {
      if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
      {
        return null;
      }

      try
      {
        IRegistryKey root = _registryWrapper.GetRootKey(path, out string subKeyPath);
        using (IRegistryKey? key = root.OpenSubKey(subKeyPath, writable: false))
        {
          return key?.GetValue(name);
        }
      }
      catch (global::System.Security.SecurityException ex)
      {
        _logger?.LogError($"Registry hive access denied: {ex.Message}");
        return null;
      }
      catch (Exception ex)
      {
        _logger?.LogError($"Registry read failed for {path}\\{name}: {ex.Message}");
        return null;
      }
    }
  }
}
