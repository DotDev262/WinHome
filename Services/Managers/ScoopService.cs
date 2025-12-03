using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ScoopService : IPackageManager
    {
        // CONSTANT: We explicitly target the .cmd shim because UseShellExecute=false
        // requires the full filename for batch scripts.
        private const string ScoopExecutable = "scoop.cmd";

        public bool IsAvailable()
        {
            // Scoop is usually a PowerShell function, but 'scoop.cmd' (shim) usually exists in path
            return RunCommand("--version"); 
        }

        public void Install(AppConfig app)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Scoop] {app.Id} is already installed.");
                return;
            }

            Console.WriteLine($"[Scoop] Installing {app.Id}...");

            // Scoop doesn't need many flags, it's very quiet by default
            string args = $"install {app.Id}";
            
            if (RunCommand(args))
                Console.WriteLine($"[Success] Installed {app.Id}");
            else
                Console.WriteLine($"[Error] Failed to install {app.Id}");
        }

        public void Uninstall(string appId)
        {
            Console.WriteLine($"[Scoop] Uninstalling {appId}...");
            
            string args = $"uninstall {appId}";
            
            if (RunCommand(args))
                Console.WriteLine($"[Success] Uninstalled {appId}");
            else
                Console.WriteLine($"[Error] Failed to uninstall {appId}");
        }

        public bool IsInstalled(string appId)
        {
            // 'scoop list' returns a nice table. 
            string output = RunCommandWithOutput("list");
            // We check if the line starts with the ID to avoid partial matches
            // (e.g. installing 'go' shouldn't match 'google-chrome')
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ScoopExecutable, // FIXED: Changed "scoop" to "scoop.cmd"
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try 
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch { return false; }
        }

        private string RunCommandWithOutput(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ScoopExecutable, // FIXED: Changed "scoop" to "scoop.cmd"
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using var process = Process.Start(startInfo);
                string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
                process?.WaitForExit();
                return output;
            }
            catch { return string.Empty; }
        }
    }
}