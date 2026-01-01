using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ScoopService : IPackageManager
    {
        private const string ScoopExecutable = "scoop.cmd";
        private readonly IProcessRunner _processRunner;

        public ScoopService(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsAvailable()
        {
            return _processRunner.RunCommand(ScoopExecutable, "--version", false);
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Scoop] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install '{app.Id}' via Scoop");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Scoop] Installing {app.Id}...");
            string args = $"install {app.Id}";

            if (!_processRunner.RunCommand(ScoopExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Scoop.");
            }
            Console.WriteLine($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would uninstall '{appId}' via Scoop");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Scoop] Uninstalling {appId}...");
            string args = $"uninstall {appId}";

            if (!_processRunner.RunCommand(ScoopExecutable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Scoop.");
            }
            Console.WriteLine($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string output = _processRunner.RunCommandWithOutput(ScoopExecutable, "list");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}