using WinHome.Models;

namespace WinHome.Interfaces
{
  public interface IConfigDriftService
  {
    Task<List<ConfigDriftResult>> DetectDriftAsync(string backupFile);
  }
}
