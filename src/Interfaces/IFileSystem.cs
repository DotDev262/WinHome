namespace WinHome.Interfaces
{
  /// <summary>Abstracts file system operations for testability.</summary>
  public interface IFileSystem
  {
    /// <summary>Returns <c>true</c> if the file at the given path exists.</summary>
    bool FileExists(string path);
    /// <summary>Returns <c>true</c> if the directory at the given path exists.</summary>
    bool DirectoryExists(string path);
    /// <summary>Reads all text from the specified file.</summary>
    string ReadAllText(string path);
    /// <summary>Writes text content to the specified file, overwriting if it exists.</summary>
    void WriteAllText(string path, string content);
    /// <summary>Creates a directory and any missing parents.</summary>
    void CreateDirectory(string path);
    /// <summary>Deletes the file at the specified path.</summary>
    void DeleteFile(string path);
    /// <summary>Recursively deletes the directory at the specified path.</summary>
    void DeleteDirectory(string path);
  }
}
