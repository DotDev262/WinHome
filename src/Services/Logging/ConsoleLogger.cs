using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly object _consoleLock = new();
        private volatile LogLevel _minLevel = LogLevel.Info;

        public void SetMinLevel(LogLevel level)
        {
            _minLevel = level;
        }

        public void Log(string message, LogLevel level)
        {
            if (level < _minLevel) return;

            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
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
