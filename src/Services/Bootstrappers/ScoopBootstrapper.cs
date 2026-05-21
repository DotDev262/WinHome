using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    /// <summary>
    /// Implements the bootstrapper logic to detect, verify, and install the Scoop package manager on the host system.
    /// </summary>
    public class ScoopBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Gets the identifying name of the package manager runtime engine.
        /// </summary>
        public string Name => "Scoop";

        /// <summary>
        /// Initializes a new instance of the <see cref="ScoopBootstrapper"/> class with a specified process execution runner.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute setup and detection commands.</param>
        public ScoopBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        /// <summary>
        /// Checks whether the Scoop package manager is currently installed on the host system by executing a version check 
        /// or falling back to searching default local and global installation directories.
        /// </summary>
        /// <returns><c>true</c> if Scoop is detected via command execution or any fallback path location exists; otherwise, <c>false</c>.</returns>
        public bool IsInstalled()
        {
            if (_processRunner.RunCommand("scoop", "--version", false)) return true;

            // Fallback for fresh installs where PATH isn't updated yet
            string[] searchPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData", "scoop", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("SCOOP") ?? "", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("SCOOP_GLOBAL") ?? "", "shims", "scoop.cmd")
            };

            foreach (var path in searchPaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) return true;
            }

            return false;
        }

        /// <summary>
        /// Installs the Scoop package manager by adjusting the current user script execution policy and deploying via the official script.
        /// Includes automated recursive retry logic for transient network name resolution failures.
        /// </summary>
        /// <param name="dryRun">If set to <c>true</c>, simulates the installation steps and logs output without modifying the system environment.</param>
        /// <exception cref="Exception">Thrown if the process fails to start or if the installation pipeline exits with errors.</exception>
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

            // Set execution policy first, then install Scoop
            // This fixes the "cannot be loaded because running scripts is disabled" error
            string command = "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force; irm get.scoop.sh -outfile install.ps1; .\\install.ps1 -RunAsAdmin; if (Test-Path .\\install.ps1) { Remove-Item .\\install.ps1 }";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{command}\"",
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
                // If it's just a DNS resolution error, it might be transient or need a retry
                if (error.Contains("remote name could not be resolved"))
                {
                    Console.WriteLine("[Bootstrapper] Network error resolving get.scoop.sh. Retrying in 10 seconds...");
                    Thread.Sleep(10000);
                    // One recursive retry
                    Install(false);
                    return;
                }
                throw new Exception($"Failed to install {Name}: {error}");
            }

            Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
        }
    }
}