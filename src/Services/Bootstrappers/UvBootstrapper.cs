using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    public class UvBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;
        public string Name => "uv";

        public UvBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsInstalled()
        {
            return _processRunner.RunCommand("uv", "--version", false);
        }

        public void Install(bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install {Name} (Python Manager)");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Bootstrapper] Installing {Name} via Scoop...");

            string scoopPath = GetScoopPath();

            var psi = new ProcessStartInfo
            {
                FileName = scoopPath,
                Arguments = "install uv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                _processRunner.RunProcessWithStartInfo(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bootstrapper] Error installing {Name}: {ex.Message}");
                throw;
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }

        private string GetScoopPath()
        {
            string scoopPath = "scoop.cmd";
            string userScoop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
            string globalScoop = Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? "C:\\ProgramData", "scoop", "shims", "scoop.cmd");

            if (File.Exists(userScoop)) scoopPath = userScoop;
            else if (File.Exists(globalScoop)) scoopPath = globalScoop;

            return scoopPath;
        }
    }
}
