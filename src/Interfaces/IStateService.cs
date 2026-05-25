using System.Collections.Generic;
using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IStateService
    {
        StateData LoadState();      // returns StateData
        void SaveState(StateData state);    // saves StateData
        void MarkAsApplied(string item);
        void TrackSystemSettingOriginal(string settingKey, object originalValue); // store original settings before applying
        void RemoveSystemSettingOriginal(string settingKey);    // clean up when settings are removed
        object? GetSystemSettingOriginal(string settingKey);    // retrieve original for reverting
        void BackupState(string backupPath);    //  backup copy of state file
        void RestoreState(string backupPath);   //  restores state from a backup file
        IEnumerable<string> ListItems();        // returns all currently applied items
    }
}
