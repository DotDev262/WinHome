using System.Text.Json;
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
                string tempFile = Path.GetTempFileName();
                try
                {
                    // export to temp file
                    // winget export -o <file> --source winget --accept-source-agreements
                    bool success = _processRunner.RunCommand("winget", $"export -o \"{tempFile}\" --source winget --accept-source-agreements", false);

                    if (success && File.Exists(tempFile))
                    {
                        string json = File.ReadAllText(tempFile);
                        apps = ParseWingetExport(json);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to export installed apps: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                return apps;
            });
        }

        public static List<AppConfig> ParseWingetExport(string json)
        {
            var apps = new List<AppConfig>();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("Sources", out JsonElement sources))
                        {
                            foreach (var source in sources.EnumerateArray())
                            {
                                if (source.TryGetProperty("Packages", out JsonElement packages))
                                {
                                    foreach (var pkg in packages.EnumerateArray())
                                    {
                                        if (pkg.TryGetProperty("PackageIdentifier", out JsonElement id))
                                        {
                                            apps.Add(new AppConfig
                                            {
                                                Id = id.GetString() ?? "",
                                                Manager = "winget"
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Invalid JSON, return empty list
                }
            }
            return apps;
        }

        private GitConfig? GetGitConfig()
        {
            try
            {
                string name = _processRunner.RunAndCapture("git", "config --global user.name");
                string email = _processRunner.RunAndCapture("git", "config --global user.email");

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
    }
}
