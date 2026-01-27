using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class WslService:IWslService
    {
        public void Configure(WslConfig config, bool dryRun)
        {
            // Fix: Allow Dry Run to proceed even if WSL isn't detected yet
            if (!IsWslInstalled())
            {
                if (dryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[DryRun] WSL is not detected/active. Simulating subsequent actions...");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("[WSL] Error: WSL is not active. Run 'wsl --install' in Admin Terminal and reboot.");
                    return;
                }
            }

            // 1. Global Settings (Update & Version)
            if (config.Update)
            {
                if (dryRun) Console.WriteLine("[DryRun] Would run 'wsl --update'");
                else RunWsl("--update");
            }
            
            if (config.DefaultVersion > 0)
            {
                if (dryRun) Console.WriteLine($"[DryRun] Would set WSL default version to {config.DefaultVersion}");
                else RunWsl($"--set-default-version {config.DefaultVersion}");
            }

            // 2. Distro Management (Install + Provision)
            if (config.Distros.Any())
            {
                Console.WriteLine("\n--- Configuring WSL Distros ---");
                foreach (var distro in config.Distros)
                {
                    // Step A: Install if missing
                    bool installed = EnsureDistro(distro, dryRun);

                    // Step B: Set as default if requested
                    if (!string.IsNullOrEmpty(config.DefaultDistro) && config.DefaultDistro == distro.Name)
                    {
                        if (dryRun) Console.WriteLine($"[DryRun] Would set default distro to '{distro.Name}'");
                        else RunWsl($"--set-default {distro.Name}");
                    }

                    // Step C: Run Setup Script (Only if installed or we just installed it)
                    // In dry run, we simulate this step regardless
                    if (installed || dryRun)
                    {
                        ProvisionDistro(distro, dryRun);
                    }
                }
            }
        }

        private bool EnsureDistro(WslDistroConfig distro, bool dryRun)
        {
            if (IsDistroInstalled(distro.Name))
            {
                // Already installed, so we can proceed to provisioning
                return true; 
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would install WSL Distro: {distro.Name}");
                Console.ResetColor();
                return false; // Not actually installed, so skip provisioning in real run
            }

            Console.WriteLine($"[WSL] Installing {distro.Name}...");
            Console.WriteLine("[WSL] NOTE: A new window will open for you to create your UNIX username/password.");

            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"--install -d {distro.Name}",
                UseShellExecute = true, 
                CreateNoWindow = false 
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                Console.WriteLine($"[Success] {distro.Name} installed.");
                return true;
            }
            else
            {
                Console.WriteLine($"[Error] Failed to install {distro.Name}");
                return false;
            }
        }

        private void ProvisionDistro(WslDistroConfig distro, bool dryRun)
        {
            if (string.IsNullOrEmpty(distro.SetupScript)) return;

            string localScriptPath = Path.GetFullPath(distro.SetupScript);
            if (!File.Exists(localScriptPath))
            {
                Console.WriteLine($"[WSL] Error: Script not found {localScriptPath}");
                return;
            }

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DryRun] Would execute '{Path.GetFileName(localScriptPath)}' inside {distro.Name}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"[WSL] Provisioning {distro.Name} with {Path.GetFileName(localScriptPath)}...");

            try
            {
                string scriptContent = File.ReadAllText(localScriptPath).Replace("\r\n", "\n");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "wsl",
                    Arguments = $"-d {distro.Name} -- bash -s",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = global::System.Text.Encoding.UTF8 // Ensure Linux output reads correctly
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                using (var writer = process.StandardInput)
                {
                    writer.Write(scriptContent);
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"[Success] {distro.Name} configured.");
                    if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output.Trim());
                }
                else
                {
                    Console.WriteLine($"[Error] Script failed in {distro.Name}:");
                    Console.WriteLine(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to execute script: {ex.Message}");
            }
        }

        private bool IsDistroInstalled(string distroName)
        {
            string output = RunWslWithOutput("--list --verbose");
            return output.Contains(distroName, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWslInstalled()
        {
            return RunWsl("--status", silent: true);
        }

        private bool RunWsl(string args, bool silent = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = silent
            };
            try 
            {
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch { return false; }
        }

        private string RunWslWithOutput(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = global::System.Text.Encoding.Unicode
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