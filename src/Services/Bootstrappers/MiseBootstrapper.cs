using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    public class MiseBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;
        public string Name => "Mise";

        public MiseBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsInstalled()
        {
            return _processRunner.RunCommand("mise", "--version", true);
        }

        public void Install(bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install {Name} via Scoop");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Bootstrapper] Installing {Name} via Scoop...");

            if (!_processRunner.RunCommand("scoop", "install mise", false))
            {
                throw new Exception($"Failed to install {Name} using Scoop.");
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }
    }
}
