using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class StateService : IStateService
    {
        private readonly string _stateFilePath;
        private readonly ILogger _logger;
        private StateData _inMemoryState;

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
            try
            {
                // Use FileShare.ReadWrite to allow reading even if we are writing (though we lock on write)
                using var stream = File.Open(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                // Try to deserialize as new StateData format
                try
                {
                    var stateData = JsonSerializer.Deserialize<StateData>(json);
                    if (stateData != null) return stateData;
                }
                catch
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
                catch
                {
                    // Fall through
                }

                return new StateData();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[State] Could not load state: {ex.Message}");
                return new StateData();
            }
        }

        public void SaveState(StateData state)
        {
            _inMemoryState = new StateData
            {
                AppliedItems = new HashSet<string>(state.AppliedItems),
                SystemSettingOriginals = new Dictionary<string, object>(state.SystemSettingOriginals)
            };
            FlushToDisk();
        }

        public void MarkAsApplied(string item)
        {
            if (_inMemoryState.AppliedItems.Add(item))
            {
                FlushToDisk();
            }
        }

        public void TrackSystemSettingOriginal(string settingKey, object originalValue)
        {
            _inMemoryState.SystemSettingOriginals[settingKey] = originalValue;
            FlushToDisk();
        }

        public void RemoveSystemSettingOriginal(string settingKey)
        {
            if (_inMemoryState.SystemSettingOriginals.Remove(settingKey))
            {
                FlushToDisk();
            }
        }

        public object? GetSystemSettingOriginal(string settingKey)
        {
            return _inMemoryState.SystemSettingOriginals.TryGetValue(settingKey, out var value) ? value : null;
        }

        private void FlushToDisk()
        {
            try
            {
                string json = JsonSerializer.Serialize(_inMemoryState, new JsonSerializerOptions { WriteIndented = true });
                // Use FileShare.Read to prevent others from writing but allow reading
                using var stream = File.Open(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
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
            return _inMemoryState.AppliedItems;
        }
    }
}
