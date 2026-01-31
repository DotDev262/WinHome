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
            if(File.Exists(targetPath))
                File.Delete(targetPath);
            File.Delete(targetPath + ".bak");
        }

        [Fact]
        public void Apply_CreatesSymbolicLink()
        {
            // Arrange
            var sourcePath = Path.GetTempFileName();
            var targetPath = Path.GetTempFileName();
            var dotfileConfig = new DotfileConfig { Src = sourcePath, Target = targetPath };
             if(File.Exists(targetPath))
                File.Delete(targetPath);
        
            // Act
            _dotfileService.Apply(dotfileConfig, false);

            // Assert
            Assert.True(File.Exists(targetPath));
            var fileInfo = new FileInfo(targetPath);
            Assert.Equal(sourcePath, fileInfo.LinkTarget);

            // Cleanup
            File.Delete(sourcePath);
             if(File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }
}
