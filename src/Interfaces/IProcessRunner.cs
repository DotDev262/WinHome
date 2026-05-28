using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WinHome.Interfaces
{
    public interface IProcessRunner
    {
        [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
        bool RunCommand(string fileName, string arguments, bool dryRun, Action<string>? onOutput = null);

        bool RunCommand(string fileName, IEnumerable<string> arguments, bool dryRun, Action<string>? onOutput = null);

        [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
        string RunCommandWithOutput(string fileName, string args);

        string RunCommandWithOutput(string fileName, IEnumerable<string> args);

        [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
        string RunCommandWithOutput(string fileName, string args, string? standardInput);

        [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
        string RunAndCapture(string fileName, string arguments);

        string RunAndCapture(string fileName, IEnumerable<string> arguments);

        bool RunProcessWithStartInfo(ProcessStartInfo startInfo);
    }
}

