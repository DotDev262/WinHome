using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WinHome.Interfaces
{
  /// <summary>Abstraction for running external processes with security-conscious argument passing.</summary>
  public interface IProcessRunner
  {
    [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
    /// <summary>Runs a command with a raw argument string (deprecated).</summary>
    bool RunCommand(string fileName, string arguments, bool dryRun, Action<string>? onOutput = null);

    /// <summary>Runs a command with individual argument tokens to prevent injection.</summary>
    bool RunCommand(string fileName, IEnumerable<string> arguments, bool dryRun, Action<string>? onOutput = null);

    [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
    /// <summary>Runs a command and captures output using a raw argument string (deprecated).</summary>
    string RunCommandWithOutput(string fileName, string args);

    /// <summary>Runs a command and captures output using individual argument tokens.</summary>
    string RunCommandWithOutput(string fileName, IEnumerable<string> args);

    [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
    /// <summary>Runs a command with stdin input using a raw argument string (deprecated).</summary>
    string RunCommandWithOutput(string fileName, string args, string? standardInput);

    /// <summary>Runs a command with stdin input using individual argument tokens.</summary>
    string RunCommandWithOutput(string fileName, IEnumerable<string> args, string? standardInput);

    [Obsolete("Use the IEnumerable<string> overload instead to prevent command injection.")]
    /// <summary>Runs and captures process output using a raw argument string (deprecated).</summary>
    string RunAndCapture(string fileName, string arguments);

    /// <summary>Runs and captures process output using individual argument tokens.</summary>
    string RunAndCapture(string fileName, IEnumerable<string> arguments);

    /// <summary>Runs a process with a fully specified <see cref="ProcessStartInfo"/>.</summary>
    bool RunProcessWithStartInfo(ProcessStartInfo startInfo);
  }
}

