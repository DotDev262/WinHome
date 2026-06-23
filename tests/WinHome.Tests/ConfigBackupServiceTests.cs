using System;
using System.IO;
using System.Threading.Tasks;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class ConfigBackupServiceTests
  {
    [Fact]
    public async Task BackupAsync_CreatesBackupFile()
    {
      var service = new ConfigBackupService();

      var config = new Configuration
      {
        Version = "1.0"
      };

      config.Extensions["test-provider"] = new
      {
        Theme = "Dark"
      };

      string output = Path.GetTempFileName();

      try
      {
        await service.BackupAsync(
          "test-provider",
          config,
          "config.yaml",
          output);

        Assert.True(File.Exists(output));
      }
      finally
      {
        if (File.Exists(output))
        {
          File.Delete(output);
        }
      }
    }

    [Fact]
    public async Task RestoreAsync_MissingFile_ThrowsFileNotFoundException()
    {
      var service = new ConfigBackupService();

      await Assert.ThrowsAsync<FileNotFoundException>(
        () => service.RestoreAsync("does-not-exist.yaml"));
    }

    [Fact]
    public async Task BackupAsync_StoresSourcePathMetadata()
    {
      var service = new ConfigBackupService();

      var config = new Configuration
      {
        Version = "1.0"
      };

      config.Extensions["test-provider"] = new
      {
        Theme = "Dark"
      };

      string output = Path.GetTempFileName();

      try
      {
        await service.BackupAsync(
          "test-provider",
          config,
          "config.yaml",
          output);

        string content = await File.ReadAllTextAsync(output);

        Assert.Contains("sourcePath: config.yaml", content);
      }
      finally
      {
        if (File.Exists(output))
        {
          File.Delete(output);
        }
      }
    }
  }
}
