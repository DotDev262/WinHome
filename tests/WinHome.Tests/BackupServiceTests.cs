using System;
using System.IO;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class BackupServiceTests
  {
    [Fact]
    public void CreateBackup_FileExists_CreatesBackup()
    {
      string path = Path.GetTempFileName();

      try
      {
        File.WriteAllText(path, "test");

        string? backupPath = BackupService.CreateBackup(path);

        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
      }
      finally
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }

    [Fact]
    public void CreateBackup_FileMissing_ReturnsNull()
    {
      string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

      string? backupPath = BackupService.CreateBackup(path);

      Assert.Null(backupPath);
    }

    [Fact]
    public void CreateBackup_TwoCalls_CreateDifferentBackups()
    {
      string path = Path.GetTempFileName();

      try
      {
        File.WriteAllText(path, "test");

        string? firstBackup = BackupService.CreateBackup(path);
        string? secondBackup = BackupService.CreateBackup(path);

        Assert.NotNull(firstBackup);
        Assert.NotNull(secondBackup);
        Assert.NotEqual(firstBackup, secondBackup);
      }
      finally
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }
  }
}
