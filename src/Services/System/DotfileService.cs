using System.IO;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides file management routines to resolve paths, manage historical file backups, 
    /// and map external file configurations into system deployment directories via symbolic links or storage fallbacks.
    /// </summary>
    public class DotfileService : IDotfileService
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotfileService"/> class with telemetry logging brokers.
        /// </summary>
        /// <param name="logger">The diagnostic log tracker capture utility routing operations and errors.</param>
        public DotfileService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Maps an isolated configuration asset file to its designated target system directory path location, 
        /// handling existing asset preservation and linking behaviors safely.
        /// </summary>
        /// <param name="dotfile">The target data object tracking the absolute source location and environment destination paths.</param>
        /// <param name="dryRun">A conditional flag which, when <c>true</c>, outputs planned actions without executing structural mutations on disk.</param>
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
                    _logger.LogSuccess($"[Dotfile] Already linked: {Path.GetFileName(targetPath)}");
                    return;
                }

                if (dryRun)
                {
                    _logger.LogError($"[DryRun] Would link {sourcePath} -> {targetPath}");
                    return;
                }

                if (File.Exists(targetPath))
                {
                    File.Move(targetPath, targetPath + ".bak", true);
                    _logger.LogSuccess($"[Dotfile] Backup created.");
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
                    _logger.LogError($"[Dotfile] Symlink failed: {ex.Message}. Falling back to copy.");
                    File.Copy(sourcePath, targetPath, true);
                    _logger.LogSuccess($"[Success] File copied -> {targetPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Error] Dotfile failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Evaluates, expands, and normalizes shell-specific and platform-specific environment variables 
        /// or home directories down to an absolute file system target location path string.
        /// </summary>
        /// <param name="path">The unparsed structural system path string potentially containing macro tokens or platform shortcuts.</param>
        /// <returns>The fully qualified, absolute file system path tracking directly to the intended destination item.</returns>
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

        /// <summary>
        /// Determines whether a physical system destination target file path is already configured as a 
        /// valid symbolic pointer directing straight back to the expected tracking source file location.
        /// </summary>
        /// <param name="source">The literal source file location directory target path string being referenced.</param>
        /// <param name="target">The downstream deployment node point file location checked for symbolic attributes.</param>
        /// <returns><c>true</c> if the downstream node exists as a symbolic reference linking directly back to the source; otherwise, <c>false</c>.</returns>
        private bool IsAlreadyLinked(string source, string target)
        {
            if (!File.Exists(target)) return false;
            var info = new FileInfo(target);
            return info.LinkTarget == source;
        }
    }
}