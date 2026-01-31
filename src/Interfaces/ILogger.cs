namespace WinHome.Interfaces
{
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public interface ILogger
    {
        void Log(string message, LogLevel level);
        void LogInfo(string message);
        void LogSuccess(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
