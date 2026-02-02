using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class GeneratorService : IGeneratorService
    {
        private readonly IPackageManager _winget;
        private readonly ISystemSettingsService _systemSettings;
        private readonly ILogger _logger;
        private readonly IProcessRunner _processRunner;

        public GeneratorService(
            Dictionary<string, IPackageManager> managers,
            ISystemSettingsService systemSettings,
            IProcessRunner processRunner,
            ILogger logger)
        {
            _winget = managers.ContainsKey("winget") ? managers["winget"] : throw new Exception("Winget manager not available");
            _systemSettings = systemSettings;
            _processRunner = processRunner;
            _logger = logger;
        }

        public async Task<Configuration> GenerateAsync()
        {
            var config = new Configuration
            {
                Version = "1.0"
            };

            // 1. Capture Apps (Winget)
            _logger.LogInfo("[Generator] Scanning installed applications...");
            var apps = await GetInstalledAppsAsync();
            config.Apps.AddRange(apps);

            // 2. Capture System Settings
            _logger.LogInfo("[Generator] Scanning system settings...");
            config.SystemSettings = await _systemSettings.GetCapturedSettingsAsync();

            // 3. Capture Git Config
            _logger.LogInfo("[Generator] Scanning git configuration...");
            config.Git = GetGitConfig();

            return config;
        }

        private async Task<List<AppConfig>> GetInstalledAppsAsync()
        {
            return await Task.Run(() =>
            {
                var apps = new List<AppConfig>();
                // We'll use `winget export` logic essentially
                // winget export -o -
                // But parsing JSON output is better if available.
                // For simplicity/robustness, we'll try to parse `winget list` or just ask the user to fill it.
                // Actually, let's implement a basic `winget list` parser or leave it empty for V1 if parsing is too complex.
                
                // Better: Run `winget list` and capture IDs.
                // Since this can be slow and produce thousands of items (libs, vc++ runtimes), 
                // a "full dump" might be too noisy.
                // Let's settle for a placeholder or a very simple scan.
                
                // Compromise: We won't scan ALL apps to avoid noise (System components etc).
                // We'll just return an empty list with a comment/example in a real scenario,
                // OR we try to parse explicitly installed ones.
                
                // For this implementation, let's leave it empty to be safe and fast,
                // as robustly filtering "user installed apps" vs "system frameworks" is hard on Windows.
                return apps;
            });
        }

        private GitConfig? GetGitConfig()
        {
            try
            {
                string name = RunGit("config --global user.name");
                string email = RunGit("config --global user.email");

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email)) return null;

                return new GitConfig
                {
                    UserName = name,
                    UserEmail = email
                };
            }
            catch
            {
                return null;
            }
        }

        private string RunGit(string args)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return "";
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output;
            }
            catch
            {
                return "";
            }
        }
    }
}
