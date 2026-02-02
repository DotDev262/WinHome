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

            Console.WriteLine($"[Bootstrapper] Installing {Name}...");

            // Official install script for Windows
            string command = "irm https://astral.sh/uv/install.ps1 | iex";

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
                    throw new Exception($"Failed to install {Name}: {error}");
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"[Bootstrapper] Error installing {Name}: {ex.Message}");
                 throw;
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }
    }
}
