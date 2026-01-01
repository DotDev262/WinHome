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
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
                process?.WaitForExit();
                return output;
            }
            catch { return string.Empty; }
        }
    }
}
