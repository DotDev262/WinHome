using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
    /// <summary>
    /// A no-op logger used internally by services that don't have direct access to the main logger.
    /// </summary>
    public class NullLogger : ILogger
    {
        public void Log(string message, LogLevel level) { }
        public void LogInfo(string message) { }
        public void LogSuccess(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
        public void SetMinLevel(LogLevel level) { }
    }
}
