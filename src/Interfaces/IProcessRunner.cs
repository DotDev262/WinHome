namespace WinHome.Interfaces
{
    public interface IProcessRunner
    {
        bool RunCommand(string fileName, string arguments, bool dryRun, Action<string>? onOutput = null);
        string RunCommandWithOutput(string fileName, string args);
        string RunCommandWithOutput(string fileName, string args, string? standardInput);
        string RunAndCapture(string fileName, string arguments);
    }
}
