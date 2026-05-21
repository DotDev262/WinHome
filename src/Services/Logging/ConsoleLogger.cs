using WinHome.Interfaces;

namespace WinHome.Services.Logging
{
    /// <summary>
    /// Provides a thread-safe console logging implementation that outputs color-coded diagnostic messages based on severity levels.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly object _consoleLock = new();

        /// <summary>
        /// Routes a log message to the appropriate formatting method based on its designated importance level.
        /// </summary>
        /// <param name="message">The text contents or operational string descriptive detail to record.</param>
        /// <param name="level">The severity threshold determining how the message classification is visually flagged.</param>
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

        /// <summary>
        /// Writes an error alert notice to the active window standard streams marked with a red foreground presentation color highlight.
        /// </summary>
        /// <param name="message">The failure string details or exception message text to display.</param>
        public void LogError(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Writes a standard informational entry block to the active window tracking interface streams using default system text formats.
        /// </summary>
        /// <param name="message">The standard operational message track data contents to display.</param>
        public void LogInfo(string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Writes a successful operation validation notice to the standard application console streams highlighted in a green presentation layout.
        /// </summary>
        /// <param name="message">The completion message validation confirmation text details to record.</param>
        public void LogSuccess(string message)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Writes an explicit security or warning flag notification statement to the console streams using a high-visibility yellow format color highlight.
        /// </summary>
        /// <param name="message">The potential issue alert or operational warning detail context to track.</param>
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