using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
  /// <summary>Checks for and applies WinHome self-updates by downloading the latest GitHub release and performing an atomic self-replacement with rollback.</summary>
  public class UpdateService : IUpdateService
  {
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IHostApplicationLifetime _lifetime;
    private const string RepoOwner = "DotDev262";
    private const string RepoName = "WinHome";
    private const string CurrentExecutableName = "WinHome.exe";

    public UpdateService(ILogger logger, IHostApplicationLifetime lifetime, HttpClient? httpClient = null)
    {
      _logger = logger;
      _lifetime = lifetime;
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
      string? currentDir = null;
      string oldPath = string.Empty;
      string tempPath = Path.Combine(Path.GetTempPath(), $"{CurrentExecutableName}.new");
      string stagingPath = string.Empty;

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

        currentDir = Path.GetDirectoryName(currentPath);
        if (string.IsNullOrEmpty(currentDir))
        {
          _logger.LogError("[Update] Could not determine executable directory.");
          return;
        }

        stagingPath = Path.Combine(currentDir, $"{CurrentExecutableName}.staging");
        oldPath = Path.Combine(currentDir, $"{CurrentExecutableName}.old");

        _logger.LogInfo($"[Update] Downloading {release.TagName}...");
        using (var stream = await _httpClient.GetStreamAsync(asset.BrowserDownloadUrl))
        using (var fileStream = new FileStream(tempPath, FileMode.Create))
        {
          await stream.CopyToAsync(fileStream);
        }

        // Verify SHA256 hash
        string downloadedHash;
        using (var sha256 = SHA256.Create())
        using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
        {
          byte[] hashBytes = await sha256.ComputeHashAsync(fs);
          downloadedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
        }

        string? expectedHash = await GetExpectedHashAsync(release, asset);
        if (!string.IsNullOrEmpty(expectedHash))
        {
          if (!string.Equals(downloadedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
          {
            _logger.LogError($"[Update] SHA256 hash mismatch: expected {expectedHash}, got {downloadedHash}. Aborting update.");
            return;
          }
          _logger.LogSuccess("[Update] SHA256 hash verified successfully.");
        }
        else
        {
          _logger.LogWarning("[Update] No SHA256 reference hash available — skipping binary verification.");
        }

        // Copy validated binary to staging path alongside current executable
        if (File.Exists(stagingPath)) TryDelete(stagingPath);
        File.Copy(tempPath, stagingPath, overwrite: true);
        _logger.LogSuccess("[Update] Download complete. Applying update...");

        // Create timestamped backup before applying update
        backupPath = BackupService.CreateAtomicBackup(currentPath);
        if (backupPath == null)
        {
          _logger.LogError("[Update] Backup creation failed. Aborting update for safety.");
          return;
        }
        _logger.LogInfo($"[Update] Created backup at {backupPath}");

        // Transactional swap with rollback
        if (File.Exists(oldPath)) TryDelete(oldPath);

        File.Move(currentPath, oldPath);

        try
        {
          File.Move(stagingPath, currentPath);
        }
        catch (Exception swapEx)
        {
          _logger.LogError($"[Update] Swap failed during staging move: {swapEx.Message}. Rolling back...");
          TryRollback(oldPath, currentPath);
          TryDelete(stagingPath);
          _logger.LogError("[Update] Update failed — original executable restored.");
          return;
        }

        TryDelete(tempPath);

        // Verify new executable
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

        _logger.LogSuccess("[Update] Verification successful. Update applied! Starting new process...");

        // Launch cleanup script (fire-and-forget)
        LaunchCleanupScript(oldPath, currentPath);

        // Graceful shutdown
        _lifetime.StopApplication();
      }
      catch (Exception ex)
      {
        _logger.LogError($"[Update] Update failed: {ex.Message}");

        // Rollback strategy
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
        TryDelete(stagingPath);
      }
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
      string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

      using var request = new HttpRequestMessage(HttpMethod.Get, url);

      // Authenticated requests get 5000/hr vs 60/hr for unauthenticated
      string? token = Environment.GetEnvironmentVariable("WINHOME_GITHUB_TOKEN")
                      ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
      if (!string.IsNullOrEmpty(token))
      {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
      }

      var response = await _httpClient.SendAsync(request);
      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadAsStringAsync();
      return JsonSerializer.Deserialize<GitHubRelease>(json);
    }

    /// <summary>Attempts to fetch the expected SHA256 hash from a companion .sha256 asset or the asset's metadata field.</summary>
    private async Task<string?> GetExpectedHashAsync(GitHubRelease release, GitHubAsset asset)
    {
      // 1. Check inline asset metadata
      if (!string.IsNullOrEmpty(asset.Sha256))
        return asset.Sha256;

      // 2. Look for a companion .sha256 asset in the same release
      string shaAssetName = $"{CurrentExecutableName}.sha256";
      var shaAsset = release.Assets.FirstOrDefault(a => a.Name.Equals(shaAssetName, StringComparison.OrdinalIgnoreCase));
      if (shaAsset != null)
      {
        try
        {
          string content = await _httpClient.GetStringAsync(shaAsset.BrowserDownloadUrl);
          // Format: "<hash>  filename" or just "<hash>"
          string hash = content.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
          if (hash.Length == 64) return hash;
        }
        catch
        {
          // Companion asset not accessible — fall through
        }
      }

      return null;
    }

    /// <summary>Launches a PowerShell script that deletes the .old file and verifies the new executable is launchable.</summary>
    private void LaunchCleanupScript(string oldPath, string currentPath)
    {
      string scriptPath = Path.Combine(Path.GetTempPath(), $"winhome-cleanup-{Guid.NewGuid():N}.ps1");

      // Escape single quotes for PowerShell
      string escapedOldPath = oldPath.Replace("'", "''");
      string escapedScriptPath = scriptPath.Replace("'", "''");

      string script = $@"Start-Sleep -Seconds 3
$old = '{escapedOldPath}'
if (Test-Path $old) {{
  Remove-Item -LiteralPath $old -Force -ErrorAction SilentlyContinue
}}
Remove-Item -LiteralPath '{escapedScriptPath}' -Force -ErrorAction SilentlyContinue
";

      try
      {
        File.WriteAllText(scriptPath, script);

        var psi = new ProcessStartInfo
        {
          FileName = "powershell.exe",
          Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
          CreateNoWindow = true,
          UseShellExecute = false
        };

        using (var proc = Process.Start(psi))
        {
          // Brief validation: process started
          if (proc == null)
          {
            _logger.LogWarning("[Update] Cleanup script did not start; .old will be cleaned on next launch.");
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogWarning($"[Update] Could not launch cleanup script: {ex.Message}. .old will be cleaned on next launch.");
      }
    }

    /// <summary>Restores the original executable from the .old backup.</summary>
    private static void TryRollback(string oldPath, string currentPath)
    {
      try
      {
        if (File.Exists(oldPath) && !File.Exists(currentPath))
        {
          File.Move(oldPath, currentPath);
        }
      }
      catch
      {
        // Best-effort rollback
      }
    }

    /// <summary>Deletes a file silently if it exists.</summary>
    private static void TryDelete(string path)
    {
      try
      {
        if (File.Exists(path)) File.Delete(path);
      }
      catch
      {
        // Best-effort cleanup
      }
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
