using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class WingetService : IPackageManager
    {
        private const string WingetExecutable = "winget";
        private readonly IProcessRunner _processRunner;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public WingetService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
        }

        public bool IsAvailable() => Bootstrapper.IsInstalled();

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Winget] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install '{app.Id}' via Winget");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Winget] Installing {app.Id}...");
            string args = $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements";
            if (!string.IsNullOrEmpty(app.Source)) args += $" --source {app.Source}";

            if (!_processRunner.RunCommand(WingetExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Winget.");
            }
            Console.WriteLine($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would uninstall '{appId}' via Winget");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Winget] Uninstalling {appId}...");
            string args = $"uninstall --id {appId} -e --silent --accept-source-agreements";

            if (!_processRunner.RunCommand(WingetExecutable, args, false))
            {
                throw new Exception($"Failed to uninstall {appId} using Winget.");
            }
            Console.WriteLine($"[Success] Uninstalled {appId}");
        }

        public bool IsInstalled(string appId)
        {
            var output = _processRunner.RunCommandWithOutput(WingetExecutable, $"list -q {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}