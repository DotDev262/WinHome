namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for running external processes and capturing their output.
    /// </summary>
    public interface IProcessRunner
    {
        /// <summary>
        /// Runs an external command and returns whether it succeeded.
        /// </summary>
        /// <param name="fileName">The executable file to run.</param>
        /// <param name="arguments">The command-line arguments to pass.</param>
        /// <param name="dryRun">If <c>true</c>, simulates the operation without executing.</param>
        /// <param name="onOutput">Optional callback invoked for each line of output.</param>
        /// <returns><c>true</c> if the command exited successfully; otherwise <c>false</c>.</returns>
        bool RunCommand(string fileName, string arguments, bool dryRun, Action<string>? onOutput = null);

        /// <summary>
        /// Runs an external command and returns its standard output as a string.
        /// </summary>
        /// <param name="fileName">The executable file to run.</param>
        /// <param name="args">The command-line arguments to pass.</param>
        /// <returns>The standard output of the process.</returns>
        string RunCommandWithOutput(string fileName, string args);

        /// <summary>
        /// Runs an external command with optional standard input and returns its output.
        /// </summary>
        /// <param name="fileName">The executable file to run.</param>
        /// <param name="args">The command-line arguments to pass.</param>
        /// <param name="standardInput">Optional text to send to the process via standard input.</param>
        /// <returns>The standard output of the process.</returns>
        string RunCommandWithOutput(string fileName, string args, string? standardInput);

        /// <summary>
        /// Runs an external command and captures its full output.
        /// </summary>
        /// <param name="fileName">The executable file to run.</param>
        /// <param name="arguments">The command-line arguments to pass.</param>
        /// <returns>The captured standard output of the process.</returns>
        string RunAndCapture(string fileName, string arguments);
    }
}