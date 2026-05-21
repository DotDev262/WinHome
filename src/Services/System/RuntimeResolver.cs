using System;
using System.Diagnostics;
using System.IO;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides resolution logic for locating executable binaries and runtime environments 
    /// across varying installation patterns, including system PATH, local user application data, 
    /// and specific package manager shimming structures.
    /// </summary>
    public class RuntimeResolver : IRuntimeResolver
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeResolver"/> class.
        /// </summary>
        /// <param name="logger">The diagnostic logging utility used to record resolution attempts or failures.</param>
        public RuntimeResolver(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Attempts to locate the absolute filesystem path for a given runtime name, 
        /// utilizing multi-stage lookup strategies.
        /// </summary>
        /// <param name="runtimeName">The name of the executable or runtime identifier to resolve (e.g., "bun", "scoop", "winget").</param>
        /// <returns>The fully qualified path to the executable if discovered; otherwise, the original <paramref name="runtimeName"/>.</returns>
        public string Resolve(string runtimeName)
        {
            // 1. Check System PATH via 'where' command
            string pathMatch = GetFullPath(runtimeName);
            if (!string.IsNullOrEmpty(pathMatch))
            {
                return pathMatch;
            }

            // 2. Resolve via common Windows installation directory patterns
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            string? manualPath = runtimeName switch
            {
                "bun" => Path.Combine(localAppData, ".bun", "bin", "bun.exe"),
                "uv" => Path.Combine(localAppData, "uv", "uv.exe"),
                "scoop" => Path.Combine(userProfile, "scoop", "shims", "scoop.cmd"),
                "choco" => Path.Combine(programData, "chocolatey", "bin", "choco.exe"),
                "winget" => Path.Combine(localAppData, "Microsoft", "WindowsApps", "winget.exe"),
                _ => null
            };

            if (manualPath != null && File.Exists(manualPath)) return manualPath;
            
            // Special case for Chocolatey legacy path
            if (runtimeName == "choco" && File.Exists(@"C:\ProgramData\chocolatey\bin\choco.exe")) 
                return @"C:\ProgramData\chocolatey\bin\choco.exe";

            // 3. Fallback to Scoop Shim search
            string scoopShim = Path.Combine(userProfile, "scoop", "shims", $"{runtimeName}.exe");
            if (File.Exists(scoopShim)) return scoopShim;

            string scoopCmdShim = Path.Combine(userProfile, "scoop", "shims", $"{runtimeName}.cmd");
            if (File.Exists(scoopCmdShim)) return scoopCmdShim;

            // Return original identifier for dynamic invocation if local resolution fails
            return runtimeName;
        }

        /// <summary>
        /// Executes a shell query to locate an executable path within the system's environment PATH variable.
        /// </summary>
        /// <param name="runtimeName">The binary name to search for.</param>
        /// <returns>The first identified absolute path, or an empty string if not found.</returns>
        private string GetFullPath(string runtimeName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = runtimeName,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return string.Empty;

                string fullPath = string.Empty;
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine()?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(line)) continue;

                    // Prioritize executable formats
                    if (line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        line.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                        line.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                    {
                        fullPath = line;
                        break;
                    }

                    // Fallback to first non-empty result
                    if (string.IsNullOrEmpty(fullPath)) fullPath = line;
                }

                process.WaitForExit();
                return process.ExitCode == 0 ? fullPath : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RuntimeResolver] Failed to resolve {runtimeName}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}