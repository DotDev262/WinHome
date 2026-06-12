using System;
using System.IO;

namespace WinHome.Services.System
{
  public static class BackupService
  {
    public static string? CreateAtomicBackup(string path)
    {
      string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
      string backupPath = $"{path}.{timestamp}.bak";
      int counter = 1;
      while (true)
      {
        try
        {
          File.Copy(path, backupPath, overwrite: false);
          return backupPath;
        }
        catch (FileNotFoundException)
        {
          return null;
        }
        catch (IOException) when (counter < 10)
        {
          backupPath = $"{path}.{timestamp}.{counter++}.bak";
        }
        catch (IOException)
        {
          return null;
        }
      }
    }
  }
}
