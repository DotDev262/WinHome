using Moq;
using System;
using System.IO;
using WinHome.Interfaces;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class FileBackupServiceTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly FileBackupService _backupService;

        public FileBackupServiceTests()
        {
            _loggerMock = new Mock<ILogger>();
            _backupService = new FileBackupService(_loggerMock.Object);
        }

        [Fact]
        public void CreateBackup_FileDoesNotExist_ReturnsNull()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");

            // Act
            var result = _backupService.CreateBackup(nonExistentPath);

            // Assert
            Assert.Null(result);
            _loggerMock.Verify(l => l.LogInfo(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void CreateBackup_FileExists_CreatesBackupWithTimestamp()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var originalContent = "Original content";
            File.WriteAllText(tempFile, originalContent);

            try
            {
                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.NotNull(backupPath);
                Assert.True(File.Exists(backupPath));
                Assert.Equal(originalContent, File.ReadAllText(backupPath));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (backupPath != null && File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }

        [Fact]
        public void CreateBackup_BackupPathHasCorrectFormat()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");
            var filename = Path.GetFileName(tempFile);
            var directory = Path.GetDirectoryName(tempFile);

            try
            {
                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.NotNull(backupPath);
                var backupFilename = Path.GetFileName(backupPath);

                // Check format: filename.YYYY-MM-DD-HHMMSS.{uuid}.bak
                Assert.StartsWith(filename + ".", backupFilename);
                Assert.EndsWith(".bak", backupFilename);

                // Extract timestamp and UUID portions and validate format
                var parts = backupFilename.Replace(filename + ".", "").Replace(".bak", "").Split('.');
                Assert.Equal(2, parts.Length); // timestamp and uuid

                var timestampPart = parts[0];
                var uuidPart = parts[1];

                // Validate timestamp: YYYY-MM-DD-HHMMSS
                var timestampParts = timestampPart.Split('-');
                Assert.Equal(4, timestampParts.Length); // YYYY, MM, DD, HHMMSS

                // Validate each part
                Assert.True(int.TryParse(timestampParts[0], out var year) && year >= 2020 && year <= 2099);
                Assert.True(int.TryParse(timestampParts[1], out var month) && month >= 1 && month <= 12);
                Assert.True(int.TryParse(timestampParts[2], out var day) && day >= 1 && day <= 31);
                Assert.True(int.TryParse(timestampParts[3], out var time) && time >= 0 && time <= 235959);

                // Validate UUID: 32 hex characters
                Assert.Equal(32, uuidPart.Length);
                Assert.True(uuidPart.All(c => "0123456789abcdefABCDEF".Contains(c)));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (backupPath != null && File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }

        [Fact]
        public void CreateBackup_PreservesOriginalContent()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var originalContent = "Line 1\nLine 2\nLine 3\nSpecial chars: !@#$%^&*()";
            File.WriteAllText(tempFile, originalContent);

            try
            {
                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.NotNull(backupPath);
                var backupContent = File.ReadAllText(backupPath);
                Assert.Equal(originalContent, backupContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (backupPath != null && File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }

        [Fact]
        public void CreateBackup_LogsInfoMessage()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "content");

            try
            {
                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.NotNull(backupPath);
                _loggerMock.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("Created backup at") && s.Contains(backupPath))), Times.Once);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (backupPath != null && File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }

        [Fact]
        public void CreateBackup_MultipleBackups_AllHaveDifferentFilenames()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "content");
            var backupPaths = new System.Collections.Generic.List<string>();

            try
            {
                // Act - Create multiple backups
                // UUID ensures each backup has a unique filename even if created in the same millisecond
                for (int i = 0; i < 3; i++)
                {
                    var backupPath = _backupService.CreateBackup(tempFile);
                    if (backupPath != null)
                    {
                        backupPaths.Add(backupPath);
                    }
                }

                // Assert - All backup paths should be different due to UUID
                var uniquePaths = new System.Collections.Generic.HashSet<string>(backupPaths);
                Assert.Equal(backupPaths.Count, uniquePaths.Count);
                Assert.Equal(3, backupPaths.Count);

                // All backups should exist
                foreach (var backupPath in backupPaths)
                {
                    Assert.True(File.Exists(backupPath));
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                foreach (var backupPath in backupPaths)
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                }
            }
        }

        [Fact]
        public void CreateBackup_FileInNestedDirectory_CreatesBackupInSameDirectory()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"winhome_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "config.json");
            File.WriteAllText(tempFile, "{}");

            try
            {
                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.NotNull(backupPath);
                Assert.Equal(tempDir, Path.GetDirectoryName(backupPath));
                Assert.True(File.Exists(backupPath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void CreateBackup_OnException_LogsWarningAndReturnsNull()
        {
            // Arrange
            var readOnlyDir = Path.Combine(Path.GetTempPath(), $"readonly_{Guid.NewGuid()}");
            Directory.CreateDirectory(readOnlyDir);
            var tempFile = Path.Combine(readOnlyDir, "test.txt");
            File.WriteAllText(tempFile, "content");

            try
            {
                // Make directory read-only
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes = FileAttributes.ReadOnly;

                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.Null(backupPath);
                _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Failed to create backup"))), Times.Once);
            }
            finally
            {
                // Restore write permissions
                var dirInfo = new DirectoryInfo(readOnlyDir);
                dirInfo.Attributes = FileAttributes.Normal;

                if (Directory.Exists(readOnlyDir))
                    Directory.Delete(readOnlyDir, true);
            }
        }

        [Fact]
        public void CreateBackup_BinaryFile_PresservesContent()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var binaryContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            File.WriteAllBytes(tempFile, binaryContent);

            try
            {
                // Act
                var backupPath = _backupService.CreateBackup(tempFile);

                // Assert
                Assert.NotNull(backupPath);
                var backupContent = File.ReadAllBytes(backupPath);
                Assert.Equal(binaryContent, backupContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (backupPath != null && File.Exists(backupPath))
                    File.Delete(backupPath);
            }
        }
    }
}
