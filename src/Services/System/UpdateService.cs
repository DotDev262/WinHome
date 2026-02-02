using System.Diagnostics;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
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

        public async Task UpdateAsync()
        {
            try
            {
                var release = await GetLatestReleaseAsync();
                if (release == null)
                {
                    _logger.LogError("[Update] Could not fetch release info.");
                    return;
                }

                var asset = release.Assets.FirstOrDefault(a => a.Name.Equals(CurrentExecutableName, StringComparison.OrdinalIgnoreCase));
                if (asset == null)
                {
                    _logger.LogError($"[Update] Could not find '{CurrentExecutableName}' in the latest release.");
                    return;
                }

                string currentPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (string.IsNullOrEmpty(currentPath))
                {
                    _logger.LogError("[Update] Could not determine current executable path.");
                    return;
                }

                string tempPath = Path.Combine(Path.GetTempPath(), $"{CurrentExecutableName}.new");

                _logger.LogInfo($"[Update] Downloading {release.TagName}...");
                using (var stream = await _httpClient.GetStreamAsync(asset.BrowserDownloadUrl))
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }

                _logger.LogSuccess("[Update] Download complete. Applying update...");

                // Self-update dance:
                // 1. Rename current EXE to .old
                // 2. Move new EXE to current path
                // 3. Start new process to delete .old and verify
                
                string oldPath = currentPath + ".old";
                
                if (File.Exists(oldPath)) File.Delete(oldPath);
                
                File.Move(currentPath, oldPath);
                File.Move(tempPath, currentPath);

                _logger.LogSuccess("[Update] Update applied! Restarting...");

                // Launch a cleaner process (cmd) to delete the old file after a delay
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C timeout 2 && del \"{oldPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Update] Update failed: {ex.Message}");
                // Try to recover?
            }
        }

        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubRelease>(json);
        }

        private bool IsNewer(string latest, string current)
        {
            if (Version.TryParse(latest, out var vLatest) && Version.TryParse(current, out var vCurrent))
            {
                return vLatest > vCurrent;
            }
            return string.Compare(latest, current) > 0;
        }
    }
}
