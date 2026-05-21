using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    /// <summary>
    /// Implements the bootstrapper logic to detect, verify, and install the Chocolatey package manager on the host system.
    /// </summary>
    public class ChocolateyBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Gets the identifying name of the package manager runtime engine.
        /// </summary>
        public string Name => "Chocolatey";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChocolateyBootstrapper"/> class with a specified process execution runner.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute setup and detection commands.</param>
        public ChocolateyBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        /// <summary>
        /// Checks whether the Chocolatey package manager is currently installed on the host system by running a version command 
        /// or verifying its default path location in ProgramData.
        /// </summary>
        /// <returns><c>true</c> if Chocolatey is detected via command execution or its executable path exists; otherwise, <c>false</c>.</returns>
        public bool IsInstalled()
        {
            if (_processRunner.RunCommand("choco", "--version", false)) return true;

            string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe");
            return File.Exists(chocoPath);
        }

        /// <summary>
        /// Installs the Chocolatey package manager by executing the official remote PowerShell deployment script.
        /// Includes automated retry mechanisms for unexpected network resolution or execution errors.
        /// </summary>
        /// <param name="dryRun">If set to <c>true</c>, simulates the installation steps and logs output without modifying the host machine.</param>
        /// <exception cref="Exception">Thrown if an unrecoverable failure occurs during execution or if the installation loop finishes with a non-zero exit code.</exception>
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