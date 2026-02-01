namespace WinHome.Interfaces
{
    public interface IProcessRunner
    {
        bool RunCommand(string fileName, string args, bool dryRun, Action<string>? onOutput = null);
        string RunCommandWithOutput(string fileName, string args);
        string RunCommandWithOutput(string fileName, string args, string standardInput);
    }
}
