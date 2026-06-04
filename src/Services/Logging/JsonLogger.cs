using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
  /// <summary>Logs messages as JSON entries for structured output consumption (e.g. CI, tooling).</summary>
  public class JsonLogger : ILogger
  {
    private readonly object _lock = new();
    private readonly List<LogEntry> _logEntries = new();
    private volatile LogLevel _minLevel = LogLevel.Info;

    /// <summary>Sets the minimum log level; messages below this level are suppressed.</summary>
    public void SetMinLevel(LogLevel level)
    {
      _minLevel = level;
    }

    /// <summary>Records a JSON log entry at the given level.</summary>
    public void Log(string message, LogLevel level)
    {
      if (level < _minLevel) return;

      lock (_lock)
      {
        _logEntries.Add(new LogEntry(message, level));
      }
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

    /// <summary>Serializes all accumulated log entries as a JSON string.</summary>
    public string ToJson()
    {
      lock (_lock)
      {
        return JsonSerializer.Serialize(_logEntries, new JsonSerializerOptions { WriteIndented = true });
      }
    }
  }

  /// <summary>Represents a single log entry with message and severity level.</summary>
  public record LogEntry(string Message, LogLevel Level);
}
