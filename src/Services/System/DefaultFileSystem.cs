using System.IO;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
  /// <summary>Default implementation of <see cref="IFileSystem"/> that delegates directly to <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/>.</summary>
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
      File.WriteAllText(path, content);
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
