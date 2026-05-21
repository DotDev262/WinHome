using System.Collections.Generic;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for tracking which configuration items have been applied.
    /// </summary>
    public interface IStateService
    {
        /// <summary>
        /// Loads the current applied-state set from persistent storage.
        /// </summary>
        /// <returns>A <see cref="HashSet{T}"/> of applied item identifiers.</returns>
        HashSet<string> LoadState();

        /// <summary>
        /// Persists the given state set to storage.
        /// </summary>
        /// <param name="state">The set of applied item identifiers to save.</param>
        void SaveState(HashSet<string> state);

        /// <summary>
        /// Marks the specified item as applied in the persistent state.
        /// </summary>
        /// <param name="item">The identifier of the item to mark as applied.</param>
        void MarkAsApplied(string item);

        /// <summary>
        /// Creates a backup of the current state at the specified path.
        /// </summary>
        /// <param name="backupPath">The file path where the backup should be saved.</param>
        void BackupState(string backupPath);

        /// <summary>
        /// Restores the state from a previously created backup.
        /// </summary>
        /// <param name="backupPath">The file path of the backup to restore.</param>
        void RestoreState(string backupPath);

        /// <summary>
        /// Returns all item identifiers currently tracked in the state.
        /// </summary>
        /// <returns>An enumerable of tracked item identifiers.</returns>
        IEnumerable<string> ListItems();
    }
}