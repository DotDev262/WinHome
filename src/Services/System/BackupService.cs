using System;
using System.IO;

namespace WinHome.Services.System
{
  public static class BackupService
  {
    public static string? CreateBackup(string path)
    {
      if (!File.Exists(path))
      {
        return null;
      }

      string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");

      string backupPath = $"{path}.{timestamp}.bak";

      int counter = 1;
 
      while (File.Exists(backupPath))
      {
        backupPath = $"{path}.{timestamp}.{counter++}.bak";
      }

      File.Copy(path, backupPath, overwrite: false);

      return backupPath;
    }
  }
}
