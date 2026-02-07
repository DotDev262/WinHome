using System.Collections.Generic;

namespace WinHome.Interfaces
{
    public interface IStateService
    {
        HashSet<string> LoadState();
        void SaveState(HashSet<string> state);
        void MarkAsApplied(string item);
        void BackupState(string backupPath);
        void RestoreState(string backupPath);
        IEnumerable<string> ListItems();
    }
}