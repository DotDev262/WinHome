using System.IO;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
  /// <summary>Default implementation of <see cref="IFileSystem"/> that delegates directly to System.IO.File and System.IO.Directory.</summary>
  public class DefaultFileSystem : IFileSystem
  {
    public bool FileExists(string path)
    {
      return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
      return Directory.Exists(path);
    }

    public string ReadAllText(string path)
    {
      return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string content)
    {
      string? backupPath = BackupService.CreateAtomicBackup(path);

      if (backupPath is not null)
      {
        Console.WriteLine($"Created backup at {backupPath}");
      }

      string? dir = Path.GetDirectoryName(path);
      if (dir != null) Directory.CreateDirectory(dir);
      string tmp = path + ".tmp";
      File.WriteAllText(tmp, content);
      File.Move(tmp, path, overwrite: true);
    }
    public void CreateDirectory(string path)
    {
      Directory.CreateDirectory(path);
    }

    public void DeleteFile(string path)
    {
      File.Delete(path);
    }

    public void DeleteDirectory(string path)
    {
      Directory.Delete(path, recursive: true);
    }
  }
}
