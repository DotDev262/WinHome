using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
  public class JsonLogger : ILogger
  {
    private readonly LogFileWriter? _logFile;
    private readonly object _lock = new();
    private readonly List<LogEntry> _logEntries = new();
    private volatile LogLevel _minLevel = LogLevel.Info;

    public JsonLogger(LogFileWriter? logFile = null)
    {
      _logFile = logFile;
    }

    public void SetMinLevel(LogLevel level)
    {
      _minLevel = level;
    }

    public void Log(string message, LogLevel level)
    {
      if (level < _minLevel) return;

      lock (_lock)
      {
        _logEntries.Add(new LogEntry(message, level));
      }

      _logFile?.Write(level, message);
    }

    public void LogError(string message)
    {
      Log(message, LogLevel.Error);
    }

    public void LogInfo(string message)
    {
      Log(message, LogLevel.Info);
    }

    public void LogSuccess(string message)
    {
      Log(message, LogLevel.Success);
    }

    public void LogWarning(string message)
    {
      Log(message, LogLevel.Warning);
    }

    public string ToJson()
    {
      lock (_lock)
      {
        return JsonSerializer.Serialize(_logEntries, new JsonSerializerOptions { WriteIndented = true });
      }
    }
  }

  public record LogEntry(string Message, LogLevel Level);
}
