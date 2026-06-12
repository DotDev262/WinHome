using System;
using System.IO;
using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
    /// <summary>Logs messages to the console with color-coded output for each severity level.</summary>
    public class ConsoleLogger : ILogger
    {
        private readonly object _consoleLock = new();
        private volatile LogLevel _minLevel = LogLevel.Info;

        private readonly string? _logFilePath;

        public ConsoleLogger(string? logFilePath = null)
        {
            _logFilePath = logFilePath;
        }

        /// <summary>Sets the minimum log level; messages below this level are suppressed.</summary>
        public void SetMinLevel(LogLevel level)
        {
            _minLevel = level;
        }

        private void WriteToFile(string message, LogLevel level)
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
                return;

            var formatted =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            File.AppendAllText(
                _logFilePath,
                formatted + Environment.NewLine);
        }

        /// <summary>Logs a message at the given level with appropriate console coloring.</summary>
        public void Log(string message, LogLevel level)
        {
            if (level < _minLevel)
                return;

            WriteToFile(message, level);

            switch (level)
            {
                case LogLevel.Trace:
                    WriteTrace(message);
                    break;

                case LogLevel.Debug:
                    WriteDebug(message);
                    break;

                case LogLevel.Info:
                    WriteInfo(message);
                    break;

                case LogLevel.Success:
                    WriteSuccess(message);
                    break;

                case LogLevel.Warning:
                    WriteWarning(message);
                    break;

                case LogLevel.Error:
                    WriteError(message);
                    break;

                default:
                    WriteInfo(message);
                    break;
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

        private void WriteError(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private void WriteTrace(string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"[Trace] {message}");
            }
        }

        private void WriteDebug(string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"[Debug] {message}");
            }
        }

        private void WriteInfo(string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(message);
            }
        }

        private void WriteSuccess(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private void WriteWarning(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
    }
}
