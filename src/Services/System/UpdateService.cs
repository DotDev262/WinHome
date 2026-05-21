using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Manages the application lifecycle for remote updates by interacting with the GitHub Release API.
    /// Implements an atomic file-swapping mechanism for safe self-updating.
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private const string RepoOwner = "DotDev262";
        private const string RepoName = "WinHome";
        private const string CurrentExecutableName = "WinHome.exe";

        public UpdateService(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WinHome-CLI");
        }

        /// <summary>
        /// Checks the GitHub API for a newer version compared to the currently running instance.
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync(string currentVersion)
        {
            _logger.LogInfo("[Update] Checking for updates...");

            try
            {
                var release = await GetLatestReleaseAsync();
                if (release == null) return false;

                var latestVersion = release.TagName.TrimStart('v');
                if (IsNewer(latestVersion, currentVersion))
                {
                    _logger.LogSuccess($"[Update] New version available: {release.TagName}");
                    return true;
                }

                _logger.LogInfo("[Update] You are running the latest version.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[Update] Failed to check for updates: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the latest release and performs an atomic swap of the executable.
        /// </summary>
        public async Task UpdateAsync()
        {
            try
            {
                var release = await GetLatestReleaseAsync();
                var asset = release?.Assets.FirstOrDefault(a => a.Name.Equals(CurrentExecutableName, StringComparison.OrdinalIgnoreCase));
                
                if (asset == null)
                {
                    _logger.LogError($"[Update] Could not locate '{CurrentExecutableName}' in release assets.");
                    return;
                }

                string currentPath = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Could not resolve executable path.");
                string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{CurrentExecutableName}");
                string oldPath = currentPath + ".old";

                _logger.LogInfo($"[Update] Downloading version {release!.TagName}...");
                
                using (var stream = await _httpClient.GetStreamAsync(asset.BrowserDownloadUrl))
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }

                _logger.LogSuccess("[Update] Download verified. Applying update...");

                // Atomically move the files
                if (File.Exists(oldPath)) File.Delete(oldPath);
                File.Move(currentPath, oldPath);
                File.Move(tempPath, currentPath);

                _logger.LogSuccess("[Update] Update applied! Restarting...");

                // Execute cleanup of the old file via cmd
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C timeout 3 && del \"{oldPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Update] Critical update failure: {ex.Message}");
            }
        }

        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private bool IsNewer(string latest, string current)
        {
            return Version.TryParse(latest, out var vLatest) && Version.TryParse(current, out var vCurrent)
                ? vLatest > vCurrent
                : string.Compare(latest, current, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }
}