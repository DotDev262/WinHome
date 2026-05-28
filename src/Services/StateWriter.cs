using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinHome.Models;

namespace WinHome.Services
{
    // Simple resilient writer for the .winhome-state.json manifest
    public class StateWriter
    {
        private readonly string _path;
        private readonly object _lock = new();
        private readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
        private Dictionary<string, StepResult>? _cache;

        public StateWriter(string? path = null)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var winHomeDir = Path.Combine(appData, "WinHome");

            _path = path ?? Path.Combine(winHomeDir, ".winhome-state.json");
            _opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        }

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
    }
}

