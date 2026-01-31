using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class MiseService : IPackageManager
    {
        private const string MiseExecutable = "mise";
        private readonly IProcessRunner _processRunner;
        public IPackageManagerBootstrapper Bootstrapper { get; }

        public MiseService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper)
        {
            _processRunner = processRunner;
            Bootstrapper = bootstrapper;
        }

        public bool IsAvailable()
        {
            return Bootstrapper.IsInstalled();
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Mise] {app.Id} is already set globally.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would set global '{app.Id}' via Mise");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Mise] Setting global {app.Id}...");
            string args = $"use --global {app.Id} -y";

            if (!_processRunner.RunCommand(MiseExecutable, args, false))
            {
                throw new Exception($"Failed to install {app.Id} using Mise.");
            }
            Console.WriteLine($"[Success] Installed {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would remove global '{appId}' via Mise");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Mise] Removing global {appId}...");
            string args = $"unuse --global {appId}";

            if (!_processRunner.RunCommand(MiseExecutable, args, false))
            {
                throw new Exception($"Failed to remove {appId} using Mise.");
            }
            Console.WriteLine($"[Success] Removed {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string output = _processRunner.RunCommandWithOutput(MiseExecutable, "ls --global --current");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }
    }
}