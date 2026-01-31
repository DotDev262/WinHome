using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    public class WingetBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;
        public string Name => "Winget";

        public WingetBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsInstalled()
        {
            return _processRunner.RunCommand("winget", "--version", false);
        }

        public void Install(bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] {Name} is a system component and cannot be installed automatically.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Bootstrapper] {Name} is a system component. If it's not available, please enable it in Windows settings.");
        }
    }
}
