using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.Managers
{
    public class MiseService : IPackageManager
    {
        // On Windows, mise is typically a single binary 'mise.exe'
        private const string MiseExecutable = "mise";

        public bool IsAvailable()
        {
            return RunCommand("version");
        }

        public void Install(AppConfig app)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Mise] {app.Id} is already set globally.");
                return;
            }

            Console.WriteLine($"[Mise] Setting global {app.Id}...");

            // 'mise use --global' adds it to the global config and installs it if missing
            string args = $"use --global {app.Id}";

            if (RunCommand(args))
                Console.WriteLine($"[Success] Installed {app.Id}");
            else
                Console.WriteLine($"[Error] Failed to install {app.Id}");
        }

        public void Uninstall(string appId)
        {
            Console.WriteLine($"[Mise] Removing global {appId}...");

            // 'unuse' removes it from the global config
            string args = $"uninstall {appId}";

            if (RunCommand(args))
                Console.WriteLine($"[Success] Removed {appId}");
            else
                Console.WriteLine($"[Error] Failed to remove {appId}");
        }

        public bool IsInstalled(string appId)
        {
            // 'mise ls --global --current' lists tools currently active in the global scope
            // We check if the tool ID appears in the output
            string output = RunCommandWithOutput("ls --global --current");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = MiseExecutable,
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