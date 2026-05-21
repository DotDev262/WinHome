using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    /// <summary>
    /// Implements the bootstrapper logic to detect and install the Bun JavaScript/TypeScript runtime environment.
    /// </summary>
    public class BunBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Gets the identifying name of the package manager runtime engine.
        /// </summary>
        public string Name => "bun";

        /// <summary>
        /// Initializes a new instance of the <see cref="BunBootstrapper"/> class with a specified process execution runner.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute setup and detection commands.</param>
        public BunBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        /// <summary>
        /// Checks whether the Bun runtime environment is currently installed on the host system by querying its version.
        /// </summary>
        /// <returns><c>true</c> if the Bun executable responds successfully; otherwise, <c>false</c>.</returns>
        public bool IsInstalled()
        {
            return _processRunner.RunCommand("bun", "--version", false);
        }

        /// <summary>
        /// Installs the Bun runtime environment using the Scoop command-line installer shims layout.
        /// </summary>
        /// <param name="dryRun">If set to <c>true</c>, simulates the installation steps and logs output without altering system environment state.</param>
        /// <throws cref="Exception">Thrown if the runtime engine fails to start execution or returns a non-zero exit validation code.</throws>
        public void Install(bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install {Name} (JS/TS Runtime)");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Bootstrapper] Installing {Name} via Scoop...");

            string scoopPath = "scoop.cmd";
            string userScoop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
            string globalScoop = Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? "C:\\ProgramData", "scoop", "shims", "scoop.cmd");

            if (File.Exists(userScoop)) scoopPath = userScoop;
            else if (File.Exists(globalScoop)) scoopPath = globalScoop;

            var psi = new ProcessStartInfo
            {
                FileName = scoopPath,
                Arguments = "install bun",
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