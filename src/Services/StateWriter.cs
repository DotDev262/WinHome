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

        public StateWriter(string? path = null)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var winHomeDir = Path.Combine(appData, "WinHome");
            if (!Directory.Exists(winHomeDir)) Directory.CreateDirectory(winHomeDir);

            _path = path ?? Path.Combine(winHomeDir, ".winhome-state.json");
            _opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        }

        public Dictionary<string, StepResult> Load()
        {
            lock (_lock)
            {
                if (!File.Exists(_path)) return new Dictionary<string, StepResult>();

                try
                {
                    var text = File.ReadAllText(_path);
                    if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, StepResult>();

                    var data = JsonSerializer.Deserialize<Dictionary<string, StepResult>>(text, _opts);
                    return data ?? new Dictionary<string, StepResult>();
                }
                catch
                {
                    // Do not throw on corrupted/invalid JSON; return empty state to allow recovery
                    return new Dictionary<string, StepResult>();
                }
            }
        }

        public void RecordStep(StepResult result)
        {
            lock (_lock)
            {
                var state = Load();
                state[result.StepId] = result;

                var tmp = _path + ".tmp";
                var serialized = JsonSerializer.Serialize(state, _opts);
                File.WriteAllText(tmp, serialized);
                if (File.Exists(_path))
                {
                    File.Replace(tmp, _path, null);
                }
                else
                {
                    File.Move(tmp, _path);
                }
            }
        }
    }
}

