using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Services.Logging;

namespace WinHome.Tests
{
  public class ConsoleLoggerTests
  {
    [Fact]
    public void DefaultMinLevel_ShowsInfoAndAbove()
    {
      var logger = new ConsoleLogger();
      var output = new StringWriter();
      var originalOut = Console.Out;
      Console.SetOut(output);

      try
      {
        logger.Log("trace msg", LogLevel.Trace);
        logger.Log("debug msg", LogLevel.Debug);
        logger.LogInfo("info msg");
        logger.LogSuccess("success msg");
        logger.LogWarning("warning msg");
        logger.LogError("error msg");

        var result = output.ToString();
        Assert.DoesNotContain("trace msg", result);
        Assert.DoesNotContain("debug msg", result);
        Assert.Contains("info msg", result);
        Assert.Contains("success msg", result);
        Assert.Contains("warning msg", result);
        Assert.Contains("error msg", result);
      }
      finally
      {
        Console.SetOut(originalOut);
      }
    }

    [Fact]
    public void Verbose_ShowsAllLevels()
    {
      var logger = new ConsoleLogger();
      logger.SetMinLevel(LogLevel.Trace);
      var output = new StringWriter();
      var originalOut = Console.Out;
      Console.SetOut(output);

      try
      {
        logger.Log("trace msg", LogLevel.Trace);
        logger.Log("debug msg", LogLevel.Debug);
        logger.LogInfo("info msg");

        var result = output.ToString();
        Assert.Contains("trace msg", result);
        Assert.Contains("debug msg", result);
        Assert.Contains("info msg", result);
      }
      finally
      {
        Console.SetOut(originalOut);
      }
    }

    [Fact]
    public void Quiet_ShowsOnlyWarningsAndErrors()
    {
      var logger = new ConsoleLogger();
      logger.SetMinLevel(LogLevel.Warning);
      var output = new StringWriter();
      var originalOut = Console.Out;
      Console.SetOut(output);

      try
      {
        logger.LogInfo("info msg");
        logger.LogSuccess("success msg");
        logger.LogWarning("warning msg");
        logger.LogError("error msg");

        var result = output.ToString();
        Assert.DoesNotContain("info msg", result);
        Assert.DoesNotContain("success msg", result);
        Assert.Contains("warning msg", result);
        Assert.Contains("error msg", result);
      }
      finally
      {
        Console.SetOut(originalOut);
      }
    }
  }

  public class JsonLoggerTests
  {
    [Fact]
    public void DefaultMinLevel_ShowsInfoAndAbove()
    {
      var logger = new JsonLogger();

      logger.Log("trace msg", LogLevel.Trace);
      logger.Log("debug msg", LogLevel.Debug);
      logger.LogInfo("info msg");
      logger.LogSuccess("success msg");
      logger.LogWarning("warning msg");
      logger.LogError("error msg");

      var json = logger.ToJson();
      Assert.DoesNotContain("trace msg", json);
      Assert.DoesNotContain("debug msg", json);
      Assert.Contains("info msg", json);
      Assert.Contains("success msg", json);
      Assert.Contains("warning msg", json);
      Assert.Contains("error msg", json);
    }

    [Fact]
    public void Verbose_ShowsAllLevels()
    {
      var logger = new JsonLogger();
      logger.SetMinLevel(LogLevel.Trace);

      logger.Log("trace msg", LogLevel.Trace);
      logger.Log("debug msg", LogLevel.Debug);
      logger.LogInfo("info msg");

      var json = logger.ToJson();
      Assert.Contains("trace msg", json);
      Assert.Contains("debug msg", json);
      Assert.Contains("info msg", json);
    }

    [Fact]
    public void Quiet_ShowsOnlyWarningsAndErrors()
    {
      var logger = new JsonLogger();
      logger.SetMinLevel(LogLevel.Warning);

      logger.LogInfo("info msg");
      logger.LogSuccess("success msg");
      logger.LogWarning("warning msg");
      logger.LogError("error msg");

      var json = logger.ToJson();
      Assert.DoesNotContain("info msg", json);
      Assert.DoesNotContain("success msg", json);
      Assert.Contains("warning msg", json);
      Assert.Contains("error msg", json);
    }
  }

  public class LogFileWriterTests
  {
    [Fact]
    public void Write_CreatesFileWithFormattedEntry()
    {
      var path = Path.Combine(Path.GetTempPath(), $"winhome-log-{Guid.NewGuid()}.log");

      try
      {
        using (var writer = new LogFileWriter(path))
        {
          writer.Write(LogLevel.Info, "hello world");
        }

        var content = File.ReadAllText(path);
        Assert.Contains("[INFO] hello world", content);
        Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] \[INFO\] hello world", content.Trim());
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
    public void Write_AppendsAcrossInstances()
    {
      var path = Path.Combine(Path.GetTempPath(), $"winhome-log-{Guid.NewGuid()}.log");

      try
      {
        using (var first = new LogFileWriter(path))
        {
          first.Write(LogLevel.Info, "first run");
        }

        using (var second = new LogFileWriter(path))
        {
          second.Write(LogLevel.Warning, "second run");
        }

        var content = File.ReadAllText(path);
        Assert.Contains("first run", content);
        Assert.Contains("second run", content);
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
    public void TryCreate_ReturnsErrorForInvalidPath()
    {
      var blockingFile = Path.GetTempFileName();
      var invalidPath = Path.Combine(blockingFile, "winhome.log");

      try
      {
        var created = LogFileWriter.TryCreate(invalidPath, out var writer, out var error);

        Assert.False(created);
        Assert.Null(writer);
        Assert.NotNull(error);
        Assert.Contains("Could not open log file", error);
      }
      finally
      {
        File.Delete(blockingFile);
      }
    }
  }

  public class ConsoleLoggerLogFileTests
  {
    [Fact]
    public void Log_WritesToConsoleAndFile()
    {
      var path = Path.Combine(Path.GetTempPath(), $"winhome-log-{Guid.NewGuid()}.log");
      var output = new StringWriter();
      var originalOut = Console.Out;

      try
      {
        using var logFile = new LogFileWriter(path);
        var logger = new ConsoleLogger(logFile);
        Console.SetOut(output);

        logger.LogInfo("info msg");
        logger.LogError("error msg");

        var consoleResult = output.ToString();
        Assert.Contains("info msg", consoleResult);
        Assert.Contains("error msg", consoleResult);

        var fileResult = File.ReadAllText(path);
        Assert.Contains("[INFO] info msg", fileResult);
        Assert.Contains("[ERROR] error msg", fileResult);
      }
      finally
      {
        Console.SetOut(originalOut);
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }
  }

  public class JsonLoggerLogFileTests
  {
    [Fact]
    public void Log_WritesFormattedEntriesToFile()
    {
      var path = Path.Combine(Path.GetTempPath(), $"winhome-log-{Guid.NewGuid()}.log");

      try
      {
        using var logFile = new LogFileWriter(path);
        var logger = new JsonLogger(logFile);

        logger.LogInfo("json info");
        logger.LogWarning("json warning");

        var fileResult = File.ReadAllText(path);
        Assert.Contains("[INFO] json info", fileResult);
        Assert.Contains("[WARNING] json warning", fileResult);
        Assert.Contains("json info", logger.ToJson());
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

  public class LogFilePathParserTests
  {
    [Fact]
    public void Parse_ReadsSpaceSeparatedValue()
    {
      var path = LogFilePathParser.Parse(new[] { "run", "--log-file", "winhome.log" });
      Assert.Equal("winhome.log", path);
    }

    [Fact]
    public void Parse_ReadsEqualsSyntax()
    {
      var path = LogFilePathParser.Parse(new[] { "--log-file=C:\\logs\\winhome.log" });
      Assert.Equal("C:\\logs\\winhome.log", path);
    }
  }
}
