using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class DefaultProcessRunner : IProcessRunner
    {
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

                process.Start();

                if (onOutput != null)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                else
                {
                    // Still read to avoid hanging
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
            catch { return false; }
        }

        public string RunCommandWithOutput(string fileName, string args)
        {
            return RunCommandWithOutput(fileName, args, null);
        }

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
    }
}
