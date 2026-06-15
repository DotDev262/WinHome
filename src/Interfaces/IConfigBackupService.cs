using WinHome.Models;

namespace WinHome.Interfaces;

public interface IConfigBackupService
{
  Task BackupAsync(Configuration config, string output);
  Task<Configuration> RestoreAsync(string input);
}
