using System;
using System.IO;
using System.Text.Json;
using Moq;
using WinHome.Interfaces;
using WinHome.Services;
using Xunit;

namespace WinHome.Tests
{
    public class StateWriterTests
    {
        [Fact]
        public void Load_CorruptedFile_ShouldNotThrowAndReturnEmpty()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
            try
            {
                File.WriteAllText(tmp, "{ invalid json }");

                var writer = new StateWriter(tmp);
                var loaded = writer.Load();

                Assert.NotNull(loaded);
                Assert.Empty(loaded);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void RecordStep_ShouldWriteAndLoad()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
            try
            {
                var writer = new StateWriter(tmp);

                var step = new WinHome.Models.StepResult
                {
                    StepId = "winget:TestApp",
                    StepType = "app",
                    StepName = "TestApp",
                    Status = WinHome.Models.StepStatus.Succeeded,
                    AppliedAt = DateTime.UtcNow
                };

                writer.RecordStep(step);
                var loaded = writer.Load();

                Assert.True(loaded.ContainsKey(step.StepId));
                Assert.Equal(WinHome.Models.StepStatus.Succeeded, loaded[step.StepId].Status);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
            }
        }

        [Fact]
        public void RecordStep_CreatesTimestampedBackup()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_test_{Guid.NewGuid()}.json");
            try
            {
                // Create initial state file
                File.WriteAllText(tmp, "{}");

                var loggerMock = new Mock<ILogger>();
                var writer = new StateWriter(tmp, loggerMock.Object);

                var step = new WinHome.Models.StepResult
                {
                    StepId = "test:step",
                    StepType = "app",
                    StepName = "TestStep",
                    Status = WinHome.Models.StepStatus.Succeeded,
                    AppliedAt = DateTime.UtcNow
                };

                // Record step (should create backup)
                writer.RecordStep(step);

                // Find backup files (format: filename.YYYY-MM-DD-HHMMSS.{uuid}.bak)
                var backupFiles = Directory.GetFiles(
                    Path.GetDirectoryName(tmp) ?? "",
                    Path.GetFileName(tmp) + ".????-??-??-??????.*.bak"
                );

                Assert.NotEmpty(backupFiles);
                Assert.True(File.Exists(backupFiles[0]));
                
                // Verify backup was logged
                loggerMock.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("Created backup at"))), Times.Once);

                // Cleanup backups
                foreach (var backup in backupFiles)
                {
                    File.Delete(backup);
                }
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");

                // Cleanup any leftover backups
                var backupFiles = Directory.GetFiles(
                    Path.GetDirectoryName(tmp) ?? "",
                    Path.GetFileName(tmp) + ".????-??-??-??????.*.bak"
                );
                foreach (var backup in backupFiles)
                {
                    try { File.Delete(backup); } catch { }
                }
            }
        }

        [Fact]
        public void RecordStep_NoBackupForNewFile()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"winhome_state_new_{Guid.NewGuid()}.json");
            try
            {
                // Ensure file doesn't exist
                if (File.Exists(tmp))
                    File.Delete(tmp);

                var loggerMock = new Mock<ILogger>();
                var writer = new StateWriter(tmp, loggerMock.Object);

                var step = new WinHome.Models.StepResult
                {
                    StepId = "test:newfile",
                    StepType = "app",
                    StepName = "TestStep",
                    Status = WinHome.Models.StepStatus.Succeeded,
                    AppliedAt = DateTime.UtcNow
                };

                // Record step on new file
                writer.RecordStep(step);

                // No backup should be created for new files
                var backupFiles = Directory.GetFiles(
                    Path.GetDirectoryName(tmp) ?? "",
                    Path.GetFileName(tmp) + ".*.bak"
                );

                Assert.Empty(backupFiles);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
            }
        }
    }
}
