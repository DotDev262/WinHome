using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Manages the application's persistent state registry by serializing and deserializing 
    /// tracking sets to a localized JSON storage file. Provides atomic flush-to-disk capabilities 
    /// and lifecycle management for state backups.
    /// </summary>
    public class StateService : IStateService
    {
        private readonly string _stateFilePath;
        private readonly ILogger _logger;
        private HashSet<string> _inMemoryState;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateService"/> class, resolving the 
        /// persistence path from environmental variables or default configuration files.
        /// </summary>
        /// <param name="logger">The diagnostic logging utility used to record state load/save errors.</param>
        public StateService(ILogger logger)
        {
            _logger = logger;

            var envPath = Environment.GetEnvironmentVariable("WINHOME_STATE_PATH");
            _stateFilePath = string.IsNullOrEmpty(envPath) ? "winhome.state.json" : envPath;
            _inMemoryState = LoadState();
        }

        /// <summary>
        /// Loads the application state tracking set from the persistent JSON storage file.
        /// </summary>
        /// <returns>A <see cref="HashSet{T}"/> containing all currently tracked system application identifiers.</returns>
        public HashSet<string> LoadState()
        {
            if (!File.Exists(_stateFilePath)) return new HashSet<string>();
            try
            {
                // Use FileShare.ReadWrite to allow reading even if the file is currently locked by other instances
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

        /// <summary>
        /// Replaces the current in-memory tracking collection and persists the new state to disk.
        /// </summary>
        /// <param name="state">The complete collection of application state identifiers to persist.</param>
        public void SaveState(HashSet<string> state)
        {
            _inMemoryState = state;
            FlushToDisk();
        }

        /// <summary>
        /// Marks a specific item as 'applied' within the tracking set and commits the update to the persistent storage file.
        /// </summary>
        /// <param name="item">The unique identifier of the system configuration component successfully applied.</param>
        public void MarkAsApplied(string item)
        {
            if (_inMemoryState.Add(item))
            {
                FlushToDisk();
            }
        }

        /// <summary>
        /// Atomically serializes the current in-memory state collection to the JSON file on disk.
        /// </summary>
        private void FlushToDisk()
        {
            try
            {
                string json = JsonSerializer.Serialize(_inMemoryState, new JsonSerializerOptions { WriteIndented = true });
                // Use FileShare.Read to prevent other writes while we perform the create/write operation
                using var stream = File.Open(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[State] Could not save state: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a backup copy of the current state file to a secondary location.
        /// </summary>
        /// <param name="backupPath">The target filesystem path where the state backup should be stored.</param>
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

        /// <summary>
        /// Overwrites the current state file with a previously generated backup and refreshes the in-memory state.
        /// </summary>
        /// <param name="backupPath">The source path of the backup file to restore from.</param>
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

        /// <summary>
        /// Exposes the current in-memory collection of tracked system components.
        /// </summary>
        /// <returns>An enumeration of all applied application items.</returns>
        public IEnumerable<string> ListItems()
        {
            return _inMemoryState;
        }
    }
}