using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinHome.Models;

namespace WinHome.Services
{
  /// <summary>Thread-safe writer for the .winhome-state.json manifest that tracks apply step results.</summary>
  public class StateWriter
  {
    private readonly string _path;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
    private Dictionary<string, StepResult>? _cache;

    /// <summary>Initializes the writer with an optional custom path. Defaults to %LOCALAPPDATA%/WinHome/.winhome-state.json.</summary>
    public StateWriter(string? path = null)
    {
      var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      var winHomeDir = Path.Combine(appData, "WinHome");

      _path = path ?? Path.Combine(winHomeDir, ".winhome-state.json");
      _opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    /// <summary>Loads the persisted state from disk. Returns an empty dictionary if the file doesn't exist or is corrupted.</summary>
    public Dictionary<string, StepResult> Load()
    {
      lock (_lock)
      {
        if (_cache != null) return new Dictionary<string, StepResult>(_cache);

        if (!File.Exists(_path))
        {
          _cache = new Dictionary<string, StepResult>();
          return new Dictionary<string, StepResult>(_cache);
        }

        try
        {
          var text = File.ReadAllText(_path);
          if (string.IsNullOrWhiteSpace(text))
          {
            _cache = new Dictionary<string, StepResult>();
            return new Dictionary<string, StepResult>(_cache);
          }

          var data = JsonSerializer.Deserialize<Dictionary<string, StepResult>>(text, _opts);
          _cache = data ?? new Dictionary<string, StepResult>();
          return new Dictionary<string, StepResult>(_cache);
        }
        catch (Exception loadEx)
        {
          global::System.Diagnostics.Trace.WriteLine($"[StateWriter] State load failed, attempting backup: {loadEx.Message}");
          try
          {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var uuid = Guid.NewGuid().ToString("N");
            var backupPath = $"{_path}.corrupted.{timestamp}.{uuid}.bak";
            File.Move(_path, backupPath);
          }
          catch (Exception backupEx)
          {
            // Best-effort backup; continue with empty state if backup fails
            global::System.Diagnostics.Trace.WriteLine($"[StateWriter] Best-effort backup of corrupted state failed: {backupEx.Message}");
          }

          _cache = new Dictionary<string, StepResult>();
          return new Dictionary<string, StepResult>(_cache);
        }
      }
    }

    /// <summary>Records a step result to both cache and disk using atomic file replacement.</summary>
    public void RecordStep(StepResult result)
    {
      lock (_lock)
      {
        if (_cache == null) Load();
        if (_cache != null) _cache[result.StepId] = result;

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }

        var tmp = _path + ".tmp";
        var serialized = JsonSerializer.Serialize(_cache, _opts);
        File.WriteAllText(tmp, serialized);

        try
        {
          File.Replace(tmp, _path, null);
        }
        catch (FileNotFoundException)
        {
          File.Move(tmp, _path);
        }
      }
    }

    // Remove a step entry from the persisted state (used when cleanup uninstalls/reverts resources)
    public void RemoveStep(string stepId)
    {
      lock (_lock)
      {
        if (_cache == null) Load();
        if (_cache == null) return;

        if (!_cache.Remove(stepId)) return;

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }

        var tmp = _path + ".tmp";
        var serialized = JsonSerializer.Serialize(_cache, _opts);
        File.WriteAllText(tmp, serialized);

        try
        {
          File.Replace(tmp, _path, null);
        }
        catch (FileNotFoundException)
        {
          File.Move(tmp, _path);
        }
      }
    }
  }
}
