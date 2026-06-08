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

      string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");

      string backupPath = $"{path}.{timestamp}.bak";

      File.Copy(path, backupPath, overwrite: false);

      return backupPath;
    }
  }
}
