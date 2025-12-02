using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services
{
    public class ChocolateyService : IPackageManager
    {
        public bool IsAvailable()
        {
            return RunCommand("-v"); // 'choco -v'
        }

        public void Install(AppConfig app)
        {
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Choco] {app.Id} is already installed.");
                return;
            }

            Console.WriteLine($"[Choco] Installing {app.Id}...");
            // -y checks "yes" to all prompts
            string args = $"install {app.Id} -y";
            
            if (RunCommand(args))
                Console.WriteLine($"[Success] Installed {app.Id}");
            else
                Console.WriteLine($"[Error] Failed to install {app.Id}");
        }

        public void Uninstall(string appId)
        {
            Console.WriteLine($"[Choco] Uninstalling {appId}...");
            string args = $"uninstall {appId} -y";
            
            if (RunCommand(args))
                Console.WriteLine($"[Success] Uninstalled {appId}");
            else
                Console.WriteLine($"[Error] Failed to uninstall {appId}");
        }

        public bool IsInstalled(string appId)
        {
            // 'choco list -l -r' returns local packages in a pipe-delimited format
            // output: 7zip|19.00
            string output = RunCommandWithOutput($"list -l -r {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        private bool RunCommand(string args)
        {
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