using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    public class RuntimeResolver : IRuntimeResolver
    {
        private readonly ILogger _logger;

        public RuntimeResolver(ILogger logger)
        {
            _logger = logger;
        }

        public string Resolve(string runtimeName)
        {
            // 1. Check PATH and get full path
            string pathMatch = GetFullPath(runtimeName);
            if (!string.IsNullOrEmpty(pathMatch))
            {
                return pathMatch;
            }

            // 2. Common Windows paths (fallback)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (runtimeName == "bun")
            {
                string bunPath = Path.Combine(localAppData, ".bun", "bin", "bun.exe");
                if (File.Exists(bunPath)) return bunPath;
            }
            else if (runtimeName == "uv")
            {
                string uvPath = Path.Combine(localAppData, "uv", "uv.exe");
                if (File.Exists(uvPath)) return uvPath;
            }
            else if (runtimeName == "scoop")
            {
                string scoopMainShim = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
                if (File.Exists(scoopMainShim)) return scoopMainShim;
            }
            else if (runtimeName == "choco")
            {
                string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe");
                if (File.Exists(chocoPath)) return chocoPath;
                
                string chocoPathAlt = @"C:\ProgramData\chocolatey\bin\choco.exe";
                if (File.Exists(chocoPathAlt)) return chocoPathAlt;
            }
            else if (runtimeName == "winget")
            {
                string wingetPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", "winget.exe");
                if (File.Exists(wingetPath)) return wingetPath;
            }

            // 3. Scoop Shims
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string scoopShim = Path.Combine(userProfile, "scoop", "shims", $"{runtimeName}.exe");
            if (File.Exists(scoopShim)) return scoopShim;
            
            string scoopCmdShim = Path.Combine(userProfile, "scoop", "shims", $"{runtimeName}.cmd");
            if (File.Exists(scoopCmdShim)) return scoopCmdShim;

            // Return original name and hope for the best (or it will fail and trigger bootstrapping)
            return runtimeName;
        }

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

                    // Prioritize .exe, .cmd, .bat
                    if (line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        line.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                        line.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                    {
                        fullPath = line;
                        break;
                    }
                    
                    // Fallback to first found if nothing else
                    if (string.IsNullOrEmpty(fullPath)) fullPath = line;
                }
                
                process.WaitForExit();
                return process.ExitCode == 0 ? fullPath : string.Empty;
            }
            catch { return string.Empty; }
        }
    }
}
