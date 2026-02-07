using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class StateService : IStateService
    {
        private readonly string _stateFilePath;
        private readonly ILogger _logger;
        private HashSet<string> _inMemoryState;

        public StateService(ILogger logger)
        {
            _logger = logger;
            
            var envPath = Environment.GetEnvironmentVariable("WINHOME_STATE_PATH");
            _stateFilePath = string.IsNullOrEmpty(envPath) ? "winhome.state.json" : envPath;
            _inMemoryState = LoadState();
        }

        public HashSet<string> LoadState()
        {
            if (!File.Exists(_stateFilePath)) return new HashSet<string>();
            try
            {
                // Use FileShare.ReadWrite to allow reading even if we are writing (though we lock on write)
                using var stream = File.Open(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[State] Could not load state: {ex.Message}");
                return new HashSet<string>();
            }
        }

        public void SaveState(HashSet<string> state)
        {
            _inMemoryState = state;
            FlushToDisk();
        }

        public void MarkAsApplied(string item)
        {
            if (_inMemoryState.Add(item))
            {
                FlushToDisk();
            }
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
            return _inMemoryState;
        }
    }
}