namespace WinHome.Interfaces
{
  /// <summary>Defines severity levels for log messages.</summary>
  public enum LogLevel
  {
    /// <summary>Detailed diagnostic information for developers.</summary>
    Trace = -2,
    /// <summary>Debug messages useful during development.</summary>
    Debug = -1,
    /// <summary>Informational messages about normal operations.</summary>
    Info = 0,
    /// <summary>Indicates a successful operation.</summary>
    Success = 1,
    /// <summary>Warnings about potential issues that are not errors.</summary>
    Warning = 2,
    /// <summary>Error messages indicating failures.</summary>
    Error = 3
  }

  /// <summary>Abstraction for logging with configurable severity levels.</summary>
  public interface ILogger
  {
    /// <summary>Logs a message at the specified level.</summary>
    void Log(string message, LogLevel level);
    /// <summary>Logs an informational message.</summary>
    void LogInfo(string message);
    /// <summary>Logs a success message.</summary>
    void LogSuccess(string message);
    /// <summary>Logs a warning message.</summary>
    void LogWarning(string message);
    /// <summary>Logs an error message.</summary>
    void LogError(string message);
    /// <summary>Sets the minimum log level; messages below this level are suppressed.</summary>
    void SetMinLevel(LogLevel level);
  }
}
