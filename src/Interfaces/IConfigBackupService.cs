using WinHome.Models;

namespace WinHome.Interfaces;

public interface IConfigBackupService
{
  Task BackupAsync(
      string provider,
      Configuration config,
      string output);

  Task<(string Provider, object? Settings)> RestoreAsync(
      string input);
}
