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
            if (_processRunner.RunCommand("mise", "--version", false)) return true;

            string scoopMise = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "mise.exe");
            return File.Exists(scoopMise);
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

            string scoopExec = "scoop";
            if (!_processRunner.RunCommand("scoop", "--version", false))
            {
                string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
                if (File.Exists(fallback)) scoopExec = fallback;
            }

            if (!_processRunner.RunCommand(scoopExec, "install mise", false))
            {
                throw new Exception($"Failed to install {Name} using Scoop.");
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }
    }
}
