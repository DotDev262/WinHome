using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class DefaultProcessRunner : IProcessRunner
    {
        public bool RunCommand(string fileName, string args, bool dryRun)
        {
            if (dryRun) return true;

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
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
                RedirectStandardInput = standardInput != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                if (process == null) return string.Empty;

                if (standardInput != null)
                {
                    using var writer = process.StandardInput;
                    writer.Write(standardInput);
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch { return string.Empty; }
        }
    }
}
