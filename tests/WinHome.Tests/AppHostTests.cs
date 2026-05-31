using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Services.Logging;
using Xunit;

namespace WinHome.Tests;

public class AppHostTests
{
  [Fact]
  public void ConfigureServices_ShouldRegisterConsoleLogger_ByDefaults()
  {
    // Arrange
    var args = new string[] { };

    // Act
    using var host = AppHost.CreateHost(args);
    var logger = host.Services.GetRequiredService<ILogger>();

    // Assert
    Assert.IsType<ConsoleLogger>(logger);
  }

  [Fact]
  public void ConfigureServices_ShouldRegisterJsonLogger_WhenJsonFlagIsPresent()
  {
    // Arrange
    var args = new string[] { "--json" };

    // Act
    using var host = AppHost.CreateHost(args);
    var logger = host.Services.GetRequiredService<ILogger>();

    // Assert
    Assert.IsType<JsonLogger>(logger);
  }

  [Fact]
  public void ConfigureServices_ShouldRegisterJsonLogger_WhenJsonFlagIsPresentWithOtherArgs()
  {
    // Arrange
    var args = new string[] { "--dry-run", "--json", "--debug" };

    // Act
    using var host = AppHost.CreateHost(args);
    var logger = host.Services.GetRequiredService<ILogger>();

    // Assert
    Assert.IsType<JsonLogger>(logger);
  }

  [Fact]
  public void CreateHost_ThrowsForInvalidLogFilePath()
  {
    var blockingFile = Path.GetTempFileName();
    var invalidPath = Path.Combine(blockingFile, "winhome.log");

    try
    {
      var ex = Assert.Throws<InvalidOperationException>(() =>
      {
        using var host = AppHost.CreateHost(new[] { "--log-file", invalidPath });
      });

      Assert.Contains("Could not open log file", ex.Message);
    }
    finally
    {
      File.Delete(blockingFile);
    }
  }

  [Fact]
  public void CreateHost_RegistersConsoleLoggerWithLogFile()
  {
    var path = Path.Combine(Path.GetTempPath(), $"winhome-log-{Guid.NewGuid()}.log");

    try
    {
      using var host = AppHost.CreateHost(new[] { "--log-file", path });
      var logger = host.Services.GetRequiredService<ILogger>();

      Assert.IsType<ConsoleLogger>(logger);
      Assert.NotNull(host.Services.GetService<LogFileWriter>());
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
