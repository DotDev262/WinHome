using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides concrete operations to inspect the active host operating system environment, 
    /// scanning for installed package identifiers, registry configurations, and shell settings 
    /// to generate state schemas.
    /// </summary>
    public class GeneratorService : IGeneratorService
    {
        private readonly IPackageManager _winget;
        private readonly ISystemSettingsService _systemSettings;
        private readonly ILogger _logger;
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorService"/> class with package discovery dependencies and shell execution runtimes.
        /// </summary>
        /// <param name="managers">A keyed tracking directory containing available downstream system installer tool configurations.</param>
        /// <param name="systemSettings">The system property capture component extracting platform specific configurations.</param>
        /// <param name="processRunner">The low-level OS shell execution utility handling binary streams.</param>
        /// <param name="logger">The diagnostic log tracker capture utility routing system statuses or errors.</param>
        /// <exception cref="Exception">Thrown when a required fundamental package manager driver collection key is missing from dependencies.</exception>
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

        /// <summary>
        /// Queries the local machine across application boundaries, aggregating environment deltas into a consolidated configuration baseline object pass.
        /// </summary>
        /// <returns>An asynchronous task containing a mapped, immutable representation model of the compiled <see cref="Configuration"/> properties.</returns>
        public async Task<Configuration> GenerateAsync()
        {
            var config = new Configuration
            {
                Version = "1.0"
            };

            // 1. Capture Apps (Winget)
            _logger.LogSuccess("[Generator] Scanning installed applications...");
            var apps = await GetInstalledAppsAsync();
            config.Apps.AddRange(apps);

            // 2. Capture System Settings
            _logger.LogSuccess("[Generator] Scanning system settings...");
            config.SystemSettings = await _systemSettings.GetCapturedSettingsAsync();

            // 3. Capture Git Config
            _logger.LogSuccess("[Generator] Scanning git configuration...");
            config.Git = GetGitConfig();

            return config;
        }

        /// <summary>
        /// Exports active package indices to a sandboxed filesystem path asynchronously, allowing clean data extraction safely.
        /// </summary>
        /// <returns>A collection tracking structural application configurations matching downstream package records.</returns>
        private async Task<List<AppConfig>> GetInstalledAppsAsync()
        {
            return await Task.Run(() =>
            {
                var apps = new List<AppConfig>();
                string tempFile = Path.GetTempFileName();
                try
                {
                    // Export target states to a dynamic temp destination path on disk
                    // Command arguments verify system parameters: winget export -o <file> --source winget --accept-source-agreements
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
                    // Securely clear internal system garbage elements out of temporary tracking vectors
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                return apps;
            });
        }

        /// <summary>
        /// Parses raw unstructured winget source stream blocks into a valid data contract structure, handling unexpected content formatting breaks safely.
        /// </summary>
        /// <param name="json">The plain JSON layout string representation stream produced directly by an export loop execution pass.</param>
        /// <returns>A list containing mapped <see cref="AppConfig"/> objects successfully read from the source schema layout.</returns>
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
                    // Swallowing structural formatting variations gracefully protects execution pipeline integrity
                }
            }
            return apps;
        }

        /// <summary>
        /// Requests explicit data values out of global environment developer parameters using native shell process checks.
        /// </summary>
        /// <returns>A configured object mapping active workspace details if available; otherwise, <c>null</c>.</returns>
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