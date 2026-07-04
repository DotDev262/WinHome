using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
  /// <summary>Logs messages as JSON entries for structured output consumption and optional file persistence.</summary>
  public class JsonLogger : ILogger
  {
    private readonly object _lock = new();
    private readonly List<LogEntry> _logEntries = new();
    private volatile LogLevel _minLevel = LogLevel.Info;
    private readonly string? _logFilePath;

    public JsonLogger(string? logFilePath = null)
    {
      _logFilePath = logFilePath;
      if (!string.IsNullOrEmpty(_logFilePath))
      {
        try
        {
          var directory = Path.GetDirectoryName(_logFilePath);
          if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          {
            Directory.CreateDirectory(directory);
          }
        }
        catch
        {
          // Fallback gracefully if directory creation fails
        }
      }
    }

    /// <summary>Sets the minimum log level; messages below this level are suppressed.</summary>
    public void SetMinLevel(LogLevel level)
    {
      _minLevel = level;
    }

    /// <summary>Records a JSON log entry at the given level with optional file persistence.</summary>
    public void Log(string message, LogLevel level)
    {
      if (level < _minLevel) return;

      lock (_lock)
      {
        _logEntries.Add(new LogEntry(message, level));
      }

      AppendToFile(message, level);
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

    private void AppendToFile(string message, LogLevel level)
    {
      if (string.IsNullOrEmpty(_logFilePath)) return;

      lock (_lock)
      {
        try
        {
          var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
          var logLine = $"[ {timestamp} ] [ {level.ToString().ToUpper()} ] {message}{Environment.NewLine}";
          File.AppendAllText(_logFilePath, logLine);
        }
        catch
        {
          // Suppress runtime file write exceptions gracefully
        }
      }
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
