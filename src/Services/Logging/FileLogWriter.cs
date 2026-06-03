using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
  /// <summary>Appends human-readable log entries to a configured file.</summary>
  internal sealed class FileLogWriter : IDisposable
  {
    private readonly object _lock = new();
    private string? _path;

    public void SetLogFile(string? path)
    {
      if (string.IsNullOrWhiteSpace(path)) return;

      lock (_lock)
      {
        try
        {
          var fullPath = Path.GetFullPath(path);
          var directory = Path.GetDirectoryName(fullPath);
          if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          {
            throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
          }

          using var stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
          _path = fullPath;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
          throw new IOException($"Unable to open log file '{path}': {ex.Message}", ex);
        }
      }
    }

    public void Write(string message, LogLevel level)
    {
      lock (_lock)
      {
        if (_path is null) return;

        File.AppendAllText(_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {message}{Environment.NewLine}");
      }
    }

    public void Dispose()
    {
      lock (_lock)
      {
        _path = null;
      }
    }
  }
}
