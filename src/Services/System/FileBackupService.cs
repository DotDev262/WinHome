using System;
using System.IO;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Utility service for creating timestamped backups before file overwrites.
    /// Ensures consistent backup creation across all file write operations.
    /// </summary>
    public class FileBackupService
    {
        private readonly ILogger _logger;

        public FileBackupService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a timestamped backup of a file before it gets overwritten.
        /// Only creates a backup if the target file already exists.
        /// Backup format: filename.YYYY-MM-DD-HHMMSS.{uuid}.bak
        /// </summary>
        /// <param name="filePath">The path to the file that will be overwritten</param>
        /// <returns>The backup file path if backup was created, null if file didn't exist</returns>
        public string? CreateBackup(string filePath)
        {
            try
            {
                // Only backup if the file exists
                if (!File.Exists(filePath))
                {
                    return null;
                }

                // Generate timestamped backup path: filename.YYYY-MM-DD-HHMMSS.{uuid}.bak
                // UUID ensures uniqueness even if multiple backups created in same second
                string directory = Path.GetDirectoryName(filePath) ?? "";
                string filename = Path.GetFileName(filePath);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                string uuid = Guid.NewGuid().ToString("N"); // 32 hex digits without hyphens
                string backupPath = Path.Combine(directory, $"{filename}.{timestamp}.{uuid}.bak");

                // Create backup by copying the file
                File.Copy(filePath, backupPath, overwrite: false);
                _logger.LogInfo($"Created backup at {backupPath}");

                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to create backup for '{filePath}': {ex.Message}");
                return null;
            }
        }
    }
}
