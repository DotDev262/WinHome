using WinHome.Interfaces;

namespace WinHome.Services.Logging;

public sealed class LogFileWriter : IDisposable
{
  private readonly StreamWriter _writer;
  private readonly object _lock = new();

  public LogFileWriter(string path)
  {
    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    _writer = new StreamWriter(path, append: true) { AutoFlush = true };
  }

  public static bool TryCreate(string path, out LogFileWriter? writer, out string? error)
  {
    try
    {
      writer = new LogFileWriter(path);
      error = null;
      return true;
    }
    catch (Exception ex)
    {
      writer = null;
      error = $"Could not open log file '{path}': {ex.Message}";
      return false;
    }
  }

  public void Write(LogLevel level, string message)
  {
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    var levelName = level.ToString().ToUpperInvariant();

    lock (_lock)
    {
      _writer.WriteLine($"[{timestamp}] [{levelName}] {message}");
    }
  }

  public void Dispose()
  {
    _writer.Dispose();
  }
}
