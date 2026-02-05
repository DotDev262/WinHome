using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class StateService : IStateService
    {
        private const string StateFileName = "winhome.state.json";
        private readonly ILogger _logger;

        public StateService(ILogger logger)
        {
            _logger = logger;
        }

        public HashSet<string> LoadState()
        {
            if (!File.Exists(StateFileName)) return new HashSet<string>();
            try
            {
                string json = File.ReadAllText(StateFileName);
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
            try
            {
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFileName, json);
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
                if (File.Exists(StateFileName))
                {
                    File.Copy(StateFileName, backupPath, true);
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
                    File.Copy(backupPath, StateFileName, true);
                    _logger.LogSuccess($"[State] State restored from: {backupPath}");
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
            return LoadState();
        }
    }
}
