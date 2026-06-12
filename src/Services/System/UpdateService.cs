using System.Diagnostics;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
  /// <summary>Checks for and applies WinHome self-updates by downloading the latest GitHub release and performing a self-replacement dance.</summary>
  public class UpdateService : IUpdateService
  {
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private const string RepoOwner = "DotDev262";
    private const string RepoName = "WinHome";
    private const string CurrentExecutableName = "WinHome.exe";

    public UpdateService(ILogger logger, HttpClient? httpClient = null)
    {
      _logger = logger;
      _httpClient = httpClient ?? new HttpClient();

      if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
      {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WinHome-CLI");
      }
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
      string? backupPath = null;
      string currentPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
      string oldPath = currentPath + ".old";
      string tempPath = Path.Combine(Path.GetTempPath(), $"{CurrentExecutableName}.new");

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

        if (string.IsNullOrEmpty(currentPath))
        {
          _logger.LogError("[Update] Could not determine current executable path.");
          return;
        }

        _logger.LogInfo($"[Update] Downloading {release.TagName}...");
        using (var stream = await _httpClient.GetStreamAsync(asset.BrowserDownloadUrl))
        using (var fileStream = new FileStream(tempPath, FileMode.Create))
        {
          await stream.CopyToAsync(fileStream);
        }

        _logger.LogSuccess("[Update] Download complete. Applying update...");

        // Self-update dance:
        // 1. Create backup of current version
        // 2. Rename current EXE to .old
        // 3. Move new EXE to current path
        // 4. Verify new EXE works using --version
        // 5. If verified, schedule .old deletion, restart, and exit

        backupPath = BackupService.CreateAtomicBackup(currentPath);
        if (backupPath == null)
        {
          _logger.LogError("[Update] Backup creation failed. Aborting update for safety.");
          return;
        }
        _logger.LogInfo($"Created backup at {backupPath}");

        if (File.Exists(oldPath))
        {
          File.Delete(oldPath);
        }

        File.Move(currentPath, oldPath);
        File.Move(tempPath, currentPath);

        // Verification check of the new executable
        _logger.LogInfo("[Update] Verifying downloaded executable...");
        using (var checkProcess = Process.Start(new ProcessStartInfo
        {
          FileName = currentPath,
          Arguments = "--version",
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardOutput = true
        }))
        {
          if (checkProcess == null || !checkProcess.WaitForExit(TimeSpan.FromSeconds(5)) || checkProcess.ExitCode != 0)
          {
            throw new Exception("New executable verification failed (failed to launch or returned non-zero code).");
          }
        }

        _logger.LogSuccess("[Update] Verification successful. Update applied!");

        // Launch a cleaner process (cmd) to delete the old file after a delay
        try
        {
          Process.Start(new ProcessStartInfo
          {
            FileName = "cmd.exe",
            Arguments = $"/C timeout 2 && del \"{oldPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
          });
        }
        catch (Exception deleteEx)
        {
          _logger.LogWarning($"[Update] Failed to start old file cleanup process: {deleteEx.Message}");
        }

        _logger.LogSuccess("[Update] Restarting...");
        try
        {
          var restartProcess = Process.Start(new ProcessStartInfo
          {
            FileName = currentPath,
            UseShellExecute = true
          });
          if (restartProcess == null)
          {
            throw new Exception("Process.Start returned null.");
          }
          _logger.LogSuccess("[Update] New instance launched. Exiting current process.");
          global::System.Environment.Exit(0);
        }
        catch (Exception restartEx)
        {
          _logger.LogError($"[Update] Failed to launch new instance: {restartEx.Message}");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError($"[Update] Update failed: {ex.Message}");

        // Rollback strategy:
        try
        {
          if (!string.IsNullOrEmpty(currentPath))
          {
            _logger.LogInfo("[Update] Attempting rollback to previous version...");

            if (File.Exists(oldPath))
            {
              if (File.Exists(currentPath)) File.Delete(currentPath);
              File.Move(oldPath, currentPath);
              _logger.LogSuccess("[Update] Rollback successful: Restored previous version from old path.");
            }
            else if (backupPath != null && File.Exists(backupPath))
            {
              if (File.Exists(currentPath)) File.Delete(currentPath);
              File.Copy(backupPath, currentPath, overwrite: true);
              _logger.LogSuccess("[Update] Rollback successful: Restored previous version from backup.");
            }
          }
        }
        catch (Exception rollbackEx)
        {
          _logger.LogError($"[Update] Rollback failed: {rollbackEx.Message}");
        }
      }
      finally
      {
        if (File.Exists(tempPath))
        {
          try { File.Delete(tempPath); } catch { }
        }
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
