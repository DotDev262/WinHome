using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    /// <summary>
    /// Implements the bootstrapper logic to detect, verify, and install the uv Python package manager on the host system.
    /// </summary>
    public class UvBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Gets the identifying name of the package manager runtime engine.
        /// </summary>
        public string Name => "uv";

        /// <summary>
        /// Initializes a new instance of the <see cref="UvBootstrapper"/> class with a specified process execution runner.
        /// </summary>
        /// <param name="processRunner">The system process runner instance used to execute setup and detection commands.</param>
        public UvBootstrapper(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        /// <summary>
        /// Checks whether the uv Python manager is currently installed on the host system by querying its version.
        /// </summary>
        /// <returns><c>true</c> if the uv executable responds successfully; otherwise, <c>false</c>.</returns>
        public bool IsInstalled()
        {
            return _processRunner.RunCommand("uv", "--version", false);
        }

        /// <summary>
        /// Installs the uv Python manager using the Scoop command-line installer shims layout.
        /// </summary>
        /// <param name="dryRun">If set to <c>true</c>, simulates the installation steps and logs output without modifying the system environment.</param>
        /// <exception cref="Exception">Thrown if the installation process fails to start or exits with a non-zero exit code.</exception>
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

            string scoopPath = "scoop.cmd";
            string userScoop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
            string globalScoop = Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? "C:\\ProgramData", "scoop", "shims", "scoop.cmd");

            if (File.Exists(userScoop)) scoopPath = userScoop;
            else if (File.Exists(globalScoop)) scoopPath = globalScoop;

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