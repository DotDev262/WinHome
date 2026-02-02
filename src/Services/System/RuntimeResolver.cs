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
            // 1. Check PATH
            if (IsInPath(runtimeName))
            {
                return runtimeName;
            }

            // 2. Check Mise
            string misePath = ResolveViaMise(runtimeName);
            if (!string.IsNullOrEmpty(misePath))
            {
                return misePath;
            }

            // 3. Common Windows paths (fallback)
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

            // Return original name and hope for the best (or it will fail and trigger bootstrapping)
            return runtimeName;
        }

        private bool IsInPath(string runtimeName)
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
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch { return false; }
        }

        private string ResolveViaMise(string runtimeName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "mise.exe",
                    Arguments = $"which {runtimeName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null) return string.Empty;

                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    return output;
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
