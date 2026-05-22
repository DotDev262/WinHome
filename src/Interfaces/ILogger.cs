namespace WinHome.Interfaces
{
    public enum LogLevel
    {
        Trace = -2,
        Debug = -1,
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    public interface ILogger
    {
        void Log(string message, LogLevel level);
        void LogInfo(string message);
        void LogSuccess(string message);
        void LogWarning(string message);
        void LogError(string message);
        void SetMinLevel(LogLevel level);
    }
}
