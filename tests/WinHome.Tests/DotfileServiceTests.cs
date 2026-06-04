using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class DotfileServiceTests
  {
    private readonly Mock<ILogger> _loggerMock;
    private readonly DotfileService _dotfileService;

    public DotfileServiceTests()
    {
      _loggerMock = new Mock<ILogger>();
      _dotfileService = new DotfileService(_loggerMock.Object);
    }

    [Fact]
    public void Apply_SourceFileDoesNotExist_LogsError()
    {
      // Arrange
      var dotfileConfig = new DotfileConfig { Src = "nonexistent.txt", Target = "target.txt" };

      // Act
      _dotfileService.Apply(dotfileConfig, false);

      // Assert
      _loggerMock.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Source file not found"))), Times.Once);
    }

    [Fact]
    public void Apply_DryRun_LogsWarning()
    {
      // Arrange
      var sourcePath = Path.GetTempFileName();
      var targetPath = Path.GetTempFileName();
      var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };

      // Act
      _dotfileService.Apply(dotfileConfig, true);

      // Assert
      _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Would link"))), Times.Once);

      // Cleanup
      File.Delete(sourcePath);
      File.Delete(targetPath);
    }

    [Fact]
    public void Apply_TargetExists_CreatesBackup()
    {
      // Arrange
      var sourcePath = Path.GetTempFileName();
      var targetPath = Path.GetTempFileName();
      var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };

      // Act
      _dotfileService.Apply(dotfileConfig, false);

      // Assert
      Assert.True(File.Exists(targetPath + ".bak"));

      // Cleanup
      File.Delete(sourcePath);
      if (File.Exists(targetPath))
        File.Delete(targetPath);
      File.Delete(targetPath + ".bak");
    }

    [Fact]
    public void Apply_CreatesSymbolicLink()
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly DotfileService _dotfileService;

        public DotfileServiceTests()
        {
            _loggerMock = new Mock<ILogger>();
            _dotfileService = new DotfileService(_loggerMock.Object);
        }

        [Fact]
        public void Apply_SourceFileDoesNotExist_LogsError()
        {
            // Arrange
            var dotfileConfig = new DotfileConfig { Src = "nonexistent.txt", Target = "target.txt" };

            // Act
            _dotfileService.Apply(dotfileConfig, false);

            // Assert
            _loggerMock.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Source file not found"))), Times.Once);
        }

        [Fact]
        public void Apply_DryRun_LogsWarning()
        {
            // Arrange
            var sourcePath = Path.GetTempFileName();
            var targetPath = Path.GetTempFileName();
            var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };

            // Act
            _dotfileService.Apply(dotfileConfig, true);

            // Assert
            _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Would link"))), Times.Once);

            // Cleanup
            File.Delete(sourcePath);
            File.Delete(targetPath);
        }

        [Fact]
        public void Apply_TargetExists_CreatesTimestampedBackup()
        {
            // Arrange
            var sourcePath = Path.GetTempFileName();
            var sourceContent = "source file content";
            File.WriteAllText(sourcePath, sourceContent);
            
            var targetPath = Path.GetTempFileName();
            var originalContent = File.ReadAllText(targetPath);

            var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };

            // Act
            _dotfileService.Apply(dotfileConfig, false);

            // Assert

            // 1. Verify timestamped backup was created
            var backupFiles = Directory.GetFiles(
                Path.GetDirectoryName(targetPath) ?? "",
                Path.GetFileName(targetPath) + ".????-??-??-??????.*.bak"
            );
            Assert.NotEmpty(backupFiles);
            var backupPath = backupFiles[0];
            Assert.True(File.Exists(backupPath));
            
            // Verify backup has original content
            var backupContent = File.ReadAllText(backupPath);
            Assert.Equal(originalContent, backupContent);

            // 2. Verify target file is replaced (either symlink or copy)
            Assert.True(File.Exists(targetPath), "Target should exist after apply");
            var targetInfo = new FileInfo(targetPath);
            
            // Verify it's either a symlink to source or a copy of source
            bool isSymlink = targetInfo.LinkTarget == sourcePath;
            bool isCopied = File.ReadAllText(targetPath) == sourceContent;
            Assert.True(isSymlink || isCopied, 
                "Target should be either a symlink to source or a copy of source");

            // 3. Verify original content is only in backup, not at target location
            if (isCopied)
            {
                // If it's a copy of source, verify it's not the original content
                Assert.NotEqual(originalContent, File.ReadAllText(targetPath));
            }

            // Cleanup
            File.Delete(sourcePath);
            if (File.Exists(targetPath))
                File.Delete(targetPath);
            File.Delete(backupPath);
        }

        [Fact]
        public void Apply_CreatesSymbolicLink()
        {
            // Arrange
            var sourcePath = Path.GetTempFileName();
            var targetPath = Path.GetTempFileName();
            var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };
            if (File.Exists(targetPath))
                File.Delete(targetPath);

            // Act
            _dotfileService.Apply(dotfileConfig, false);

            // Assert
            Assert.True(File.Exists(targetPath));
            var fileInfo = new FileInfo(targetPath);
            Assert.Equal(sourcePath, fileInfo.LinkTarget);

            // Cleanup
            File.Delete(sourcePath);
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
      // Arrange
      var sourcePath = Path.GetTempFileName();
      var targetPath = Path.GetTempFileName();
      var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };
      if (File.Exists(targetPath))
        File.Delete(targetPath);

      // Act
      _dotfileService.Apply(dotfileConfig, false);

      // Assert
      Assert.True(File.Exists(targetPath));
      var fileInfo = new FileInfo(targetPath);
      if (fileInfo.LinkTarget is not null)
      {
        Assert.Equal(sourcePath, fileInfo.LinkTarget);
      }
      else
      {
        Assert.Equal(File.ReadAllText(sourcePath), File.ReadAllText(targetPath));
        _loggerMock.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Symlink failed"))), Times.Once);
      }

      // Cleanup
      File.Delete(sourcePath);
      if (File.Exists(targetPath))
        File.Delete(targetPath);
    }
  }
}
