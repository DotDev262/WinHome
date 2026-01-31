using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    public class ScoopBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;
        public string Name => "Scoop";

        public ScoopBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsInstalled()
        {
            if (_processRunner.RunCommand("scoop", "--version", false)) return true;
            
            // Fallback for fresh installs where PATH isn't updated yet
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
            return File.Exists(defaultPath);
        }

        public void Install(bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install {Name}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Bootstrapper] Installing {Name}...");

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Set-ExecutionPolicy RemoteSigned -Scope CurrentUser; irm get.scoop.sh -outfile install.ps1; .\\install.ps1 -RunAsAdmin\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new Exception($"Failed to start installer for {Name}");
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new Exception($"Failed to install {Name}: {error}");
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }
    }
}
