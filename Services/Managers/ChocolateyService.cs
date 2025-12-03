using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ChocolateyService : IPackageManager
    {
        public bool IsAvailable()
        {
            return RunCommand("-v", false); 
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Choco] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install '{app.Id}' via Chocolatey");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Choco] Installing {app.Id}...");
            
            string args = $"install {app.Id} -y";
            
            if (RunCommand(args, false))
                Console.WriteLine($"[Success] Installed {app.Id}");
            else
                Console.WriteLine($"[Error] Failed to install {app.Id}");
        }

        public void Uninstall(string appId, bool dryRun)
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would uninstall '{appId}' via Chocolatey");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Choco] Uninstalling {appId}...");
            string args = $"uninstall {appId} -y";
            
            if (RunCommand(args, false))
                Console.WriteLine($"[Success] Uninstalled {appId}");
            else
                Console.WriteLine($"[Error] Failed to uninstall {appId}");
        }

        public bool IsInstalled(string appId)
        {
            
            string output = RunCommandWithOutput($"list -l -r {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args, bool dryRun)
        {
            if (dryRun) return true;

            var startInfo = new ProcessStartInfo
            {
                FileName = "choco",
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
                FileName = "choco",
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