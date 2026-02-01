using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
    public class WingetBootstrapper : IPackageManagerBootstrapper
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger _logger;
        public string Name => "Winget";

        public WingetBootstrapper(IProcessRunner processRunner, ILogger logger)
        {
            _processRunner = processRunner;
            _logger = logger;
        }

        public bool IsInstalled()
        {
            if (_processRunner.RunCommand("winget", "--version", false)) return true;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wingetPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", "winget.exe");
            bool exists = File.Exists(wingetPath);
            if (exists) _logger.LogInfo($"[Bootstrapper] Found winget.exe at {wingetPath} but it's not in PATH.");
            return exists;
        }

        public void Install(bool dryRun)
        {
            if (dryRun)
            {
                _logger.LogWarning($"[DryRun] Would install {Name} by downloading from GitHub.");
                return;
            }

            _logger.LogInfo($"[Bootstrapper] Installing {Name}...");

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "WinHome_WingetInstall");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                string version = GetLatestVersion();
                _logger.LogInfo($"[Bootstrapper] Latest Winget version detected: {version}");

                string dependenciesUrl = $"https://github.com/microsoft/winget-cli/releases/download/{version}/DesktopAppInstaller_Dependencies.zip";
                string msixBundleUrl = $"https://github.com/microsoft/winget-cli/releases/download/{version}/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle";

                string dependenciesZip = Path.Combine(tempDir, "dependencies.zip");
                string msixBundle = Path.Combine(tempDir, "Microsoft.DesktopAppInstaller.msixbundle");

                DownloadFile(dependenciesUrl, dependenciesZip).GetAwaiter().GetResult();
                DownloadFile(msixBundleUrl, msixBundle).GetAwaiter().GetResult();

                string extractPath = Path.Combine(tempDir, "dependencies");
                ZipFile.ExtractToDirectory(dependenciesZip, extractPath);

                _logger.LogInfo("[Bootstrapper] Installing dependencies...");
                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower(); // x64, arm64, x86
                
                var files = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file).ToLower();
                    if (fileName.EndsWith(".appx") || fileName.EndsWith(".msix") || fileName.EndsWith(".appxbundle") || fileName.EndsWith(".msixbundle"))
                    {
                        // Filter by architecture to avoid noise/failures
                        if (fileName.Contains("arm64") && arch != "arm64") continue;
                        if (fileName.Contains("x64") && arch != "x64") continue;
                        if (fileName.Contains("x86") && arch != "x86" && arch != "x64") continue;

                        _logger.LogInfo($"[Bootstrapper] Installing dependency: {Path.GetFileName(file)}");
                        InstallAppPackage(file);
                    }
                }

                _logger.LogInfo("[Bootstrapper] Installing Winget msixbundle...");
                InstallAppPackage(msixBundle);

                _logger.LogInfo($"[Bootstrapper] {Name} installation commands completed.");
                _logger.LogInfo("[Bootstrapper] Waiting 5 seconds for Windows to register the App Execution Alias...");
                Thread.Sleep(5000); 
                
                // Cleanup
                try 
                { 
                    _logger.LogInfo("[Bootstrapper] Cleaning up temporary files...");
                    Directory.Delete(tempDir, true); 
                } 
                catch (Exception ex) 
                { 
                    _logger.LogWarning($"[Bootstrapper] Warning: Could not clean up temp directory: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to install {Name}: {ex.Message}", ex);
            }
        }

        private string GetLatestVersion()
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "WinHome-Bootstrapper");
            var response = client.GetAsync("https://api.github.com/repos/microsoft/winget-cli/releases/latest").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var json = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return json.RootElement.GetProperty("tag_name").GetString() ?? "v1.12.460"; // Fallback to provided version
        }

        private async Task DownloadFile(string url, string path)
        {
            _logger.LogInfo($"[Bootstrapper] Downloading {url}...");
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10); // Increase timeout for large files
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var fs = new FileStream(path, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }

        private void InstallAppPackage(string path)
        {
            // Use Add-AppxPackage via PowerShell
            string command = $"Add-AppxPackage -Path \"{path}\"";
            string output = "";
            if (!_processRunner.RunCommand("powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"", false, line => output += line + "\n"))
            {
                _logger.LogWarning($"[Bootstrapper] Warning: Package {Path.GetFileName(path)} failed to install.");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogWarning($"[Bootstrapper:Error] {output.Trim()}");
                }
            }
        }
    }
}
