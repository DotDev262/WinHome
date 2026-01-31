using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly object _consoleLock = new();

        public void Log(string message, LogLevel level)
        {
            lock (_consoleLock)
            {
                switch (level)
                {
                    case LogLevel.Info:
                        LogInfo(message);
                        break;
                    case LogLevel.Success:
                        LogSuccess(message);
                        break;
                    case LogLevel.Warning:
                        LogWarning(message);
                        break;
                    case LogLevel.Error:
                        LogError(message);
                        break;
                }
            }
        }

        public void LogError(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public void LogInfo(string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(message);
            }
        }

        public void LogSuccess(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public void LogWarning(string message)
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
