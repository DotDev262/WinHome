using System.Diagnostics;

namespace WinHome.Services
{
    public class WingetService
    {
        public void EnsureInstalled(String appId)
        {
            if (IsInstalled(appId))
            {
                Console.WriteLine($"[Skipped] {appId} is already installed");
                return;
            }

            Console.WriteLine($"[Installing] {appId}");
            string args = $"install --id {appId} -e --silent --accept-package-agreements --accept-source-agreements";
            
            bool success = RunCommand(args);

            if (success)
                Console.WriteLine($"[Success] Installed {appId}");
            else
                Console.WriteLine($"[Error] Failed to install {appId}");

        }

        private bool IsInstalled(string appId)
        {
            // "list -q" asks winget to list installed apps matching the query
            // We check if the output contains the ID.
            string output = RunCommandWithOutput($"list -q {appId}");
            return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
        }

        // Helper: Runs a command and returns TRUE if it worked (Exit Code 0)
        private bool RunCommand(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = args,
                UseShellExecute = false,  // Do not use the OS shell, just run the executable
                CreateNoWindow = true     // Hide the pop-up window
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }

        // Helper: Runs a command and returns the TEXT output (so we can read it)
        private string RunCommandWithOutput(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = args,
                RedirectStandardOutput = true, // Capture the text
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            // Read the output before waiting for exit to avoid deadlocks
            string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            process?.WaitForExit();
            return output;
        }
    }
}