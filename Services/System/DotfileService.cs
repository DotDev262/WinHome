using WinHome.Models;

namespace WinHome.Services.System
{
    public class DotfileService
    {
        public void Apply(DotfileConfig dotfile, bool dryRun)
        {
            try
            {
                string sourcePath = Path.GetFullPath(dotfile.Src);
                string targetPath = ResolvePath(dotfile.Target);

                if (!File.Exists(sourcePath))
                {
                    Console.WriteLine($"[Dotfile] Error: Source file not found: {sourcePath}");
                    return;
                }

                if (IsAlreadyLinked(sourcePath, targetPath))
                {
                    Console.WriteLine($"[Dotfile] Already linked: {Path.GetFileName(targetPath)}");
                    return;
                }

                if (dryRun)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[DryRun] Would link {sourcePath} -> {targetPath}");
                    Console.ResetColor();
                    return;
                }

                
                if (File.Exists(targetPath))
                {
                    File.Move(targetPath, targetPath + ".bak", true);
                    Console.WriteLine($"[Dotfile] Backup created.");
                }

                string? parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir)) Directory.CreateDirectory(parentDir);

                File.CreateSymbolicLink(targetPath, sourcePath);
                Console.WriteLine($"[Success] Link created -> {sourcePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Dotfile failed: {ex.Message}");
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