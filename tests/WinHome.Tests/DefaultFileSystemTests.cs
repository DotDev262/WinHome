using System;
using System.IO;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class DefaultFileSystemTests : IDisposable
  {
    private readonly DefaultFileSystem _fs = new DefaultFileSystem();
    private readonly string _tempDir;

    public DefaultFileSystemTests()
    {
      _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
      var path = Path.Combine(_tempDir, "test.txt");
      File.WriteAllText(path, "hello");
      Assert.True(_fs.FileExists(path));
    }

    [Fact]
    public void FileExists_ReturnsFalseForMissingFile()
    {
      Assert.False(_fs.FileExists(Path.Combine(_tempDir, "nope.txt")));
    }

    [Fact]
    public void DirectoryExists_ReturnsTrueForExistingDirectory()
    {
      Assert.True(_fs.DirectoryExists(_tempDir));
    }

    [Fact]
    public void DirectoryExists_ReturnsFalseForMissingDirectory()
    {
      Assert.False(_fs.DirectoryExists(Path.Combine(_tempDir, "ghost")));
    }

    [Fact]
    public void WriteAllText_And_ReadAllText_RoundTrip()
    {
      var path = Path.Combine(_tempDir, "data.txt");
      _fs.WriteAllText(path, "hello world");
      Assert.Equal("hello world", _fs.ReadAllText(path));
    }

    [Fact]
    public void ReadAllText_ThrowsForMissingFile()
    {
      var path = Path.Combine(_tempDir, "missing.txt");
      Assert.Throws<FileNotFoundException>(() => _fs.ReadAllText(path));
    }

    [Fact]
    public void CreateDirectory_CreatesNewDirectory()
    {
      var path = Path.Combine(_tempDir, "newdir");
      _fs.CreateDirectory(path);
      Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void DeleteFile_RemovesExistingFile()
    {
      var path = Path.Combine(_tempDir, "todelete.txt");
      File.WriteAllText(path, "bye");
      _fs.DeleteFile(path);
      Assert.False(File.Exists(path));
    }

    [Fact]
    public void DeleteDirectory_RemovesExistingDirectory()
    {
      var path = Path.Combine(_tempDir, "subdir");
      Directory.CreateDirectory(path);
      _fs.DeleteDirectory(path);
      Assert.False(Directory.Exists(path));
    }

    public void Dispose()
    {
      if (Directory.Exists(_tempDir))
        Directory.Delete(_tempDir, recursive: true);
    }
  }
}
