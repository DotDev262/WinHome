using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
    public class JsonLogger : ILogger
    {
        private readonly List<LogEntry> _logEntries = new();

        public void Log(string message, LogLevel level)
        {
            _logEntries.Add(new LogEntry(message, level));
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
            return JsonSerializer.Serialize(_logEntries, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public record LogEntry(string Message, LogLevel Level);
}
