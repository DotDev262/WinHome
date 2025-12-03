using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class ScoopService : IPackageManager
    {
        
        private const string ScoopExecutable = "scoop.cmd";

        public bool IsAvailable()
        {
            return RunCommand("--version", false);
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Scoop] {app.Id} is already installed.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install '{app.Id}' via Scoop");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Scoop] Installing {app.Id}...");
            string args = $"install {app.Id}";
            
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
                Console.WriteLine($"[DryRun] Would uninstall '{appId}' via Scoop");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Scoop] Uninstalling {appId}...");
            string args = $"uninstall {appId}";
            
            if (RunCommand(args, false))
                Console.WriteLine($"[Success] Uninstalled {appId}");
            else
                Console.WriteLine($"[Error] Failed to uninstall {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string output = RunCommandWithOutput("list");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args, bool dryRun)
        {
            if (dryRun) return true;

            var startInfo = new ProcessStartInfo
            {
                FileName = ScoopExecutable,
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
                FileName = ScoopExecutable,
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