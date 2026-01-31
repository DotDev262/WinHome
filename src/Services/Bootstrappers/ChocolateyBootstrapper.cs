using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    public class ChocolateyBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;
        public string Name => "Chocolatey";

        public ChocolateyBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public bool IsInstalled()
        {
            if (_processRunner.RunCommand("choco", "--version", false)) return true;

            string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe");
            return File.Exists(chocoPath);
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
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))\"",
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