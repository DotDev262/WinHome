using System.Diagnostics;
using WinHome.Models; // <--- CRITICAL: This imports the shared AppConfig

namespace WinHome.Services
{
    public class WingetService
    {
        public void EnsureInstalled(AppConfig app)
        {
            // Check if installed using the ID
            if (IsInstalled(app.Id))
            {
                Console.WriteLine($"[Skipped] {app.Id} is already installed.");
                return;
            }

            Console.WriteLine($"[Installing] {app.Id}...");

            // Build the command
            string args = $"install --id {app.Id} -e --silent --accept-package-agreements --accept-source-agreements";

            // Add source if it exists
            if (!string.IsNullOrEmpty(app.Source))
            {
                args += $" --source {app.Source}";
            }

            bool success = RunCommand(args);

            if (success)
                Console.WriteLine($"[Success] Installed {app.Id}");
            else
                Console.WriteLine($"[Error] Failed to install {app.Id}");
        }

        private bool IsInstalled(string appId)
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

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
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

            using var process = Process.Start(startInfo);
            string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            process?.WaitForExit();
            return output;
        }
    }
}