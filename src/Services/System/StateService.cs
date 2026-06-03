using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
  /// <summary>Manages persistent state on disk with in-memory caching, atomic writes, and backward compatibility with legacy state format.</summary>
  public class StateService : IStateService
  {
    private readonly string _stateFilePath;
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private StateData _inMemoryState;

    /// <summary>Initializes a new instance of <see cref="StateService"/>.</summary>
    public StateService(ILogger logger)
    {
      _logger = logger;

      var envPath = Environment.GetEnvironmentVariable("WINHOME_STATE_PATH");
      _stateFilePath = string.IsNullOrEmpty(envPath) ? "winhome.state.json" : envPath;
      _inMemoryState = LoadState();
    }

    public StateData LoadState()
    {
      if (!File.Exists(_stateFilePath)) return new StateData();

      string json;
      try
      {
        // Use FileShare.ReadWrite to allow reading even if we are writing (though we lock on write)
        using var stream = File.Open(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        json = reader.ReadToEnd();
      }
      catch (Exception ex)
      {
        _logger.LogWarning($"[State] Could not read state file '{_stateFilePath}': {ex.Message}");
        return new StateData();
      }

      if (string.IsNullOrWhiteSpace(json))
      {
        return new StateData();
      }

      // Try to deserialize as new StateData format
      try
      {
        var stateData = JsonSerializer.Deserialize<StateData>(json);
        if (stateData != null) return stateData;
      }
      catch (JsonException)
      {
        // Fall through to backward compatibility
      }

      // Backward compatibility: try to deserialize as old HashSet<string> format
      try
      {
        var legacyState = JsonSerializer.Deserialize<HashSet<string>>(json);
        if (legacyState != null)
        {
          return new StateData { AppliedItems = legacyState };
        }
      }
      catch (JsonException ex)
      {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var backupSuffix = Path.GetRandomFileName();
        var backupPath = $"{_stateFilePath}.corrupted.{timestamp}.{backupSuffix}";

        _logger.LogWarning(
            $"[State] State file '{_stateFilePath}' is corrupted: {ex.Message}. " +
            $"Backing up to '{backupPath}' and starting with empty state.");

        try
        {
          File.Move(_stateFilePath, backupPath);
        }
        catch (Exception moveEx)
        {
          _logger.LogWarning($"[State] Could not back up corrupted state file: {moveEx.Message}");
        }

        return new StateData();
      }

      return new StateData();
    }

    public void SaveState(StateData state)
    {
      lock (_sync)
      {
        _inMemoryState = new StateData
        {
          AppliedItems = new HashSet<string>(state.AppliedItems),
          SystemSettingOriginals = new Dictionary<string, object>(state.SystemSettingOriginals)
        };
        FlushToDisk();
      }
    }

    public void MarkAsApplied(string item)
    {
      lock (_sync)
      {
        if (_inMemoryState.AppliedItems.Add(item))
        {
          FlushToDisk();
        }
      }
    }

    public void RemoveApplied(string item)
    {
      lock (_sync)
      {
        if (_inMemoryState.AppliedItems.Remove(item))
        {
          FlushToDisk();
        }
      }
    }

    public void TrackSystemSettingOriginal(string settingKey, object originalValue)
    {
      lock (_sync)
      {
        _inMemoryState.SystemSettingOriginals[settingKey] = originalValue;
        FlushToDisk();
      }
    }

    public void RemoveSystemSettingOriginal(string settingKey)
    {
      lock (_sync)
      {
        if (_inMemoryState.SystemSettingOriginals.Remove(settingKey))
        {
          FlushToDisk();
        }
      }
    }

    public object? GetSystemSettingOriginal(string settingKey)
    {
      lock (_sync)
      {
        return _inMemoryState.SystemSettingOriginals.TryGetValue(settingKey, out var value) ? value : null;
      }
    }

    private void FlushToDisk()
    {
      try
      {
        string json = JsonSerializer.Serialize(_inMemoryState, new JsonSerializerOptions { WriteIndented = true });
        string tmpPath = _stateFilePath + ".tmp";

        using (var stream = File.Open(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
          writer.Write(json);
        }

        File.Move(tmpPath, _stateFilePath, overwrite: true);
      }
      catch (Exception ex)
      {
        _logger.LogWarning($"[State] Could not save state: {ex.Message}");
      }
    }

    public void BackupState(string backupPath)
    {
      try
      {
        if (File.Exists(_stateFilePath))
        {
          File.Copy(_stateFilePath, backupPath, true);
          _logger.LogSuccess($"[State] Backup created at: {backupPath}");
        }
        else
        {
          _logger.LogWarning("[State] No state file found to backup.");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"[State] Backup failed: {ex.Message}");
      }
    }

    public void RestoreState(string backupPath)
    {
      try
      {
        if (File.Exists(backupPath))
        {
          File.Copy(backupPath, _stateFilePath, true);
          _logger.LogSuccess($"[State] State restored from: {backupPath}");
          _inMemoryState = LoadState();
        }
        else
        {
          _logger.LogError($"[State] Backup file not found: {backupPath}");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"[State] Restore failed: {ex.Message}");
      }
    }

    public IEnumerable<string> ListItems()
    {
      lock (_sync)
      {
        return _inMemoryState.AppliedItems.ToList();
      }
    }
  }
}
