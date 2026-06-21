using System;
using System.IO;
using System.Threading.Tasks;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class ConfigDriftServiceTests
  {
    [Fact]
    public async Task DetectDriftAsync_MissingBackup_Throws()
    {
      var service = new ConfigDriftService();

      await Assert.ThrowsAsync<FileNotFoundException>(
          () => service.DetectDriftAsync("missing.yaml"));
    }

    [Fact]
    public async Task DetectDriftAsync_NoDrift_ReturnsEmptyList()
    {
      var service = new ConfigDriftService();

      string backupFile = Path.GetTempFileName();
      string configFile = Path.Combine(
          Directory.GetCurrentDirectory(),
          "config.yaml");

      try
      {
        await File.WriteAllTextAsync(
            backupFile,
            "provider: test-provider\n" +
            "version: 1.0\n" +
            "sourcePath: config.yaml\n" +
            "createdAt: 2026-01-01\n" +
            "settings:\n" +
            "  theme: Dark\n");

        await File.WriteAllTextAsync(
            configFile,
            "version: 1.0\n" +
            "extensions:\n" +
            "  test-provider:\n" +
            "    theme: Dark\n");

        var result = await service.DetectDriftAsync(backupFile);

        Assert.Empty(result);
      }
      finally
      {
        if (File.Exists(backupFile))
          File.Delete(backupFile);

        if (File.Exists(configFile))
          File.Delete(configFile);
      }
    }

    [Fact]
    public async Task DetectDriftAsync_WhenSettingsDiffer_ReturnsDrift()
    {
      var service = new ConfigDriftService();

      string backupFile = Path.GetTempFileName();
      string configFile = Path.Combine(
          Directory.GetCurrentDirectory(),
          "config.yaml");

      try
      {
        await File.WriteAllTextAsync(
            backupFile,
            "provider: test-provider\n" +
            "version: 1.0\n" +
            "sourcePath: config.yaml\n" +
            "createdAt: 2026-01-01\n" +
            "settings:\n" +
            "  theme: Dark\n");

        await File.WriteAllTextAsync(
            configFile,
            "version: 1.0\n" +
            "extensions:\n" +
            "  test-provider:\n" +
            "    theme: Light\n");

        var result = await service.DetectDriftAsync(backupFile);

        Assert.Single(result);
      }
      finally
      {
        if (File.Exists(backupFile))
          File.Delete(backupFile);

        if (File.Exists(configFile))
          File.Delete(configFile);
      }
    }
  }
}
