using System.Diagnostics;

namespace WinHome.Interfaces
{
    public interface IProcessRunner
    {
        bool RunCommand(string fileName, string arguments, bool dryRun, Action<string>? onOutput = null);
        bool RunCommand(string fileName, IEnumerable<string> arguments, bool dryRun, Action<string>? onOutput = null);
        string RunCommandWithOutput(string fileName, string args);
        string RunCommandWithOutput(string fileName, IEnumerable<string> args);
        string RunCommandWithOutput(string fileName, string args, string? standardInput);
        string RunAndCapture(string fileName, string arguments);
        bool RunProcessWithStartInfo(ProcessStartInfo startInfo);
    }
}
