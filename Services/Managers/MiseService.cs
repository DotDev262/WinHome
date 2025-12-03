using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class MiseService : IPackageManager
    {
        private const string MiseExecutable = "mise";

        public bool IsAvailable()
        {
            return RunCommand("version", false);
        }

        public void Install(AppConfig app, bool dryRun)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Mise] {app.Id} is already set globally.");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would set global '{app.Id}' via Mise");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Mise] Setting global {app.Id}...");
            string args = $"use --global {app.Id} -y";

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
                Console.WriteLine($"[DryRun] Would remove global '{appId}' via Mise");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[Mise] Removing global {appId}...");
            string args = $"unuse --global {appId}";

            if (RunCommand(args, false))
                Console.WriteLine($"[Success] Removed {appId}");
            else
                Console.WriteLine($"[Error] Failed to remove {appId}");
        }

        public bool IsInstalled(string appId)
        {
            string output = RunCommandWithOutput("ls --global --current");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args, bool dryRun)
        {
            if (dryRun) return true;

            var startInfo = new ProcessStartInfo
            {
                FileName = MiseExecutable,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            try
            {
                using var process = Process.Start(startInfo);
                string errorOutput = process?.StandardError.ReadToEnd() ?? string.Empty;
                process?.WaitForExit();

                if (process?.ExitCode != 0 && !string.IsNullOrWhiteSpace(errorOutput))
                {
                    Console.WriteLine($"[Mise Error] {errorOutput.Trim()}");
                }

                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"[System Error] Could not start mise: {ex.Message}");
                return false; 
            }
        }

        private string RunCommandWithOutput(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = MiseExecutable,
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