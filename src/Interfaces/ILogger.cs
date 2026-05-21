namespace WinHome.Interfaces
{
    /// <summary>
    /// Specifies the severity level of a log message.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>General informational message.</summary>
        Info,

        /// <summary>Indicates a successful operation.</summary>
        Success,

        /// <summary>Indicates a potential issue that is not critical.</summary>
        Warning,

        /// <summary>Indicates a critical failure or error.</summary>
        Error
    }

    /// <summary>
    /// Defines a contract for logging messages at various severity levels.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message at the specified severity level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The severity level of the message.</param>
        void Log(string message, LogLevel level);

        /// <summary>Logs an informational message.</summary>
        /// <param name="message">The message to log.</param>
        void LogInfo(string message);

        /// <summary>Logs a success message.</summary>
        /// <param name="message">The message to log.</param>
        void LogSuccess(string message);

        /// <summary>Logs a warning message.</summary>
        /// <param name="message">The message to log.</param>
        void LogWarning(string message);

        /// <summary>Logs an error message.</summary>
        /// <param name="message">The message to log.</param>
        void LogError(string message);
    }
}