using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using WinHome.Services.Logging;

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
        private readonly string _path;
        private readonly object _lock = new();
        private readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
        private Dictionary<string, StepResult>? _cache;
        private readonly FileBackupService _backupService;
      var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      var winHomeDir = Path.Combine(appData, "WinHome");

      _path = path ?? Path.Combine(winHomeDir, ".winhome-state.json");
      _opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

            _path = path ?? Path.Combine(winHomeDir, ".winhome-state.json");
            _opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            
            // Initialize backup service with a no-op logger since StateWriter is internal
            _backupService = new FileBackupService(new NullLogger());
        }

        public StateWriter(string? path, ILogger logger) : this(path)
        {
            // Allow injection of logger for testing
            _backupService = new FileBackupService(logger);
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
        catch
        {
          // Do not throw on corrupted/invalid JSON; return empty state to allow recovery
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

                // Create timestamped backup before overwriting
                _backupService.CreateBackup(_path);

                var tmp = _path + ".tmp";
                var serialized = JsonSerializer.Serialize(_cache, _opts);
                File.WriteAllText(tmp, serialized);
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

