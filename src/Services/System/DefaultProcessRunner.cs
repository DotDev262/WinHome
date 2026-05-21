using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Implements a low-level OS shell execution utility to launch, manage, write to, and parse standard 
    /// output and error streams from external operating system binary processes.
    /// </summary>
    public class DefaultProcessRunner : IProcessRunner
    {
        /// <summary>
        /// Executes an external operating system file command asynchronously with optional output hooks and safety execution time windows.
        /// </summary>
        /// <param name="fileName">The absolute path string or environmental system alias pointing to the executable application target binary.</param>
        /// <param name="args">The argument command-line formatting string passed down into the spawned child process execution context.</param>
        /// <param name="dryRun">A conditional execution safety modifier flag which, when <c>true</c>, exits early and avoids launching actual processes.</param>
        /// <param name="onOutput">An optional callback delegate intercepting standard output and error log strings concurrently from line events.</param>
        /// <returns><c>true</c> if the process starts and completes execution tracking loops successfully with an exit status of <c>0</c>; otherwise, <c>false</c>.</returns>
        public bool RunCommand(string fileName, string args, bool dryRun, Action<string>? onOutput = null)
        {
            if (dryRun) return true;

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using var process = new Process { StartInfo = startInfo };
                if (onOutput != null)
                {
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) onOutput(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) onOutput(e.Data); };
                }

                if (!process.Start())
                {
                    return false;
                }

                if (onOutput != null)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                else
                {
                    // Still read to avoid hanging the underlying OS process pipe buffers
                    Task.Run(() => process.StandardOutput.ReadToEnd());
                    Task.Run(() => process.StandardError.ReadToEnd());
                }

                if (!process.WaitForExit(TimeSpan.FromMinutes(10)))
                {
                    process.Kill(true);
                    return false;
                }
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                if (onOutput != null) onOutput($"[ProcessRunner] Error starting {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Launches a target process shell command to execute synchronously and blocks the executing thread until all 
        /// available content written to standard output strings has been collected.
        /// </summary>
        /// <param name="fileName">The absolute path string or environmental system alias pointing to the executable application target binary.</param>
        /// <param name="args">The argument command-line formatting string passed down into the spawned child process execution context.</param>
        /// <returns>The accumulated standard output context string parsed from the process execution block.</returns>
        public string RunCommandWithOutput(string fileName, string args)
        {
            return RunCommandWithOutput(fileName, args, null);
        }

        /// <summary>
        /// Launches a target process shell command and feeds custom raw text matrices into the 
        /// process's standard input stream while safely capturing output strings.
        /// </summary>
        /// <param name="fileName">The absolute path string or environmental system alias pointing to the executable application target binary.</param>
        /// <param name="args">The argument command-line formatting string passed down into the spawned child process execution context.</param>
        /// <param name="standardInput">An optional payload context string immediately piped into the process input stream once started.</param>
        /// <returns>The full text block written out to standard output from the target execution loop path.</returns>
        public string RunCommandWithOutput(string fileName, string args, string? standardInput)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = standardInput != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                if (standardInput != null)
                {
                    using var writer = process.StandardInput;
                    writer.Write(standardInput);
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                if (process.WaitForExit(TimeSpan.FromMinutes(10)))
                {
                    // Process exited, wait for streams to finish with a short timeout
                    Task.WaitAll(new Task[] { outputTask, errorTask }, TimeSpan.FromSeconds(5));
                    return outputTask.Result;
                }
                else
                {
                    process.Kill(true);
                    return string.Empty;
                }
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Spawns a process synchronously, captures the standard output, trims out peripheral spacing, 
        /// and guarantees cleanup upon termination.
        /// </summary>
        /// <param name="fileName">The absolute path string or environmental system alias pointing to the executable application target binary.</param>
        /// <param name="arguments">The argument command-line formatting string passed down into the spawned child process execution context.</param>
        /// <returns>The cleaned string representation containing output text data payload elements.</returns>
        public string RunAndCapture(string fileName, string arguments)
        {
            try
            {
                var psi = new global::System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = global::System.Diagnostics.Process.Start(psi);
                if (process == null) return string.Empty;

                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}