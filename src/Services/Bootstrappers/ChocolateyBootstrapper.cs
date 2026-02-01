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

            string command = "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; " +
                             "iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null) throw new Exception($"Failed to start installer for {Name}");

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    if (error.Contains("remote name could not be resolved") || error.Contains("Operation timed out"))
                    {
                        Console.WriteLine($"[Bootstrapper] Network error installing {Name}. Retrying in 10 seconds...");
                        Thread.Sleep(10000);
                        Install(false);
                        return;
                    }
                    throw new Exception($"Failed to install {Name}: {error}");
                }
            }
            catch (Exception ex) when (!ex.Message.Contains("Failed to install"))
            {
                 Console.WriteLine($"[Bootstrapper] Unexpected error: {ex.Message}. Retrying...");
                 Thread.Sleep(5000);
                 Install(false);
                 return;
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }
    }
}