using System.Collections.Generic;
using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Manages persistent state tracking for applied configurations and original system setting values.</summary>
  public interface IStateService
  {
    /// <summary>Loads the persisted state from disk.</summary>
    StateData LoadState();
    /// <summary>Saves the state to disk.</summary>
    void SaveState(StateData state);
    /// <summary>Marks an item as having been applied.</summary>
    void MarkAsApplied(string item);
    /// <summary>Removes an item from the applied list.</summary>
    void RemoveApplied(string item);
    /// <summary>Stores the original value of a system setting before modification.</summary>
    void TrackSystemSettingOriginal(string settingKey, object originalValue);
    /// <summary>Removes the stored original value for a system setting.</summary>
    void RemoveSystemSettingOriginal(string settingKey);
    /// <summary>Retrieves the original value of a system setting for reverting.</summary>
    object? GetSystemSettingOriginal(string settingKey);
    /// <summary>Creates a backup copy of the state file.</summary>
    void BackupState(string backupPath);
    /// <summary>Restores state from a backup file.</summary>
    void RestoreState(string backupPath);
    /// <summary>Returns all currently applied item identifiers.</summary>
    IEnumerable<string> ListItems();
  }
}
