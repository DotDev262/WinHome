using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class DotfileService : IDotfileService
    {
        private readonly ILogger _logger;

        public DotfileService(ILogger logger)
        {
            _logger = logger;
        }
        public void Apply(DotfileConfig dotfile, bool dryRun)
        {
            try
            {
                string sourcePath = Path.GetFullPath(dotfile.Src);
                string targetPath = ResolvePath(dotfile.Target);

                if (!File.Exists(sourcePath))
                {
                    _logger.LogError($"[Dotfile] Error: Source file not found: {sourcePath}");
                    return;
                }

                if (IsAlreadyLinked(sourcePath, targetPath))
                {
                    _logger.LogInfo($"[Dotfile] Already linked: {Path.GetFileName(targetPath)}");
                    return;
                }

                if (dryRun)
                {
                    _logger.LogWarning($"[DryRun] Would link {sourcePath} -> {targetPath}");
                    return;
                }


                if (File.Exists(targetPath))
                {
                    File.Move(targetPath, targetPath + ".bak", true);
                    _logger.LogInfo($"[Dotfile] Backup created.");
                }

                string? parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir)) Directory.CreateDirectory(parentDir);

                try
                {
                    File.CreateSymbolicLink(targetPath, sourcePath);
                    _logger.LogSuccess($"[Success] Link created -> {targetPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Dotfile] Symlink failed: {ex.Message}. Falling back to copy.");
                    File.Copy(sourcePath, targetPath, true);
                    _logger.LogSuccess($"[Success] File copied -> {targetPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Dotfile failed: {ex.Message}");
            }
        }

        private string ResolvePath(string path)
        {
            string expanded = Environment.ExpandEnvironmentVariables(path);
            if (expanded.StartsWith("~"))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                expanded = Path.Combine(home, expanded.Substring(1).TrimStart('/', '\\'));
            }
            return Path.GetFullPath(expanded);
        }

        private bool IsAlreadyLinked(string source, string target)
        {
            if (!File.Exists(target)) return false;
            var info = new FileInfo(target);
            return info.LinkTarget == source;
        }
    }
}