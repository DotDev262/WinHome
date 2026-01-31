namespace WinHome.Interfaces
{
    public interface IProcessRunner
    {
        bool RunCommand(string fileName, string args, bool dryRun);
        string RunCommandWithOutput(string fileName, string args);
        string RunCommandWithOutput(string fileName, string args, string standardInput);
    }
}
