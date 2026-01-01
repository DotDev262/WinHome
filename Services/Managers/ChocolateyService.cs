using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ChocolateyService : IPackageManager
    {
        private const string ChocoExecutable = "choco";
        private readonly IProcessRunner _processRunner;

        public ChocolateyService(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsAvailable()
        {
            return _processRunner.RunCommand(ChocoExecutable, "-v", false);
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Choco] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install '{app.Id}' via Chocolatey");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Choco] Installing {app.Id}...");

            string args = $"install {app.Id} -y";

            if (!_processRunner.RunCommand(ChocoExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Chocolatey.");
            }

            Console.WriteLine($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would uninstall '{appId}' via Chocolatey");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Choco] Uninstalling {appId}...");
            string args = $"uninstall {appId} -y";

            if (!_processRunner.RunCommand(ChocoExecutable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Chocolatey.");
            }

            Console.WriteLine($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {

            string output = _processRunner.RunCommandWithOutput(ChocoExecutable, $"list -l -r {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}