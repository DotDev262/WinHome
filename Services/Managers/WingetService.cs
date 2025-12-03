using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    // Now implements the shared interface
    public class WingetService : IPackageManager
    {
        public bool IsAvailable()
        {
            // Simple check to see if winget exists in PATH
            return RunCommand("--help"); // 'winget --help' returns 0 if valid
        }

        // Rename 'EnsureInstalled' to 'Install' to match interface
        public void Install(AppConfig app)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Winget] {app.Id} is already installed.");
                return;
            }

            Console.WriteLine($"[Winget] Installing {app.Id}...");

            string args = $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements";
            if (!string.IsNullOrEmpty(app.Source))
            {
                args += $" --source {app.Source}";
            }

            if (RunCommand(args))
                Console.WriteLine($"[Success] Installed {app.Id}");
            else
                Console.WriteLine($"[Error] Failed to install {app.Id}");
        }

        public void Uninstall(string appId)
        {
            Console.WriteLine($"[Winget] Uninstalling {appId}...");
            string args = $"uninstall --id {appId} -e --silent --accept-source-agreements";
            
            if (RunCommand(args))
                Console.WriteLine($"[Success] Uninstalled {appId}");
            else
                Console.WriteLine($"[Error] Failed to uninstall {appId}");
        }

        public bool IsInstalled(string appId)
        {
            var output = RunCommandWithOutput($"list -q {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
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
                FileName = "winget",
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