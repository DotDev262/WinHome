using Moq;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class GeneratorServiceTests
    {
        private readonly Mock<IPackageManager> _mockWinget;
        private readonly Mock<ISystemSettingsService> _mockSettings;
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<ILogger> _mockLogger;
        private readonly GeneratorService _generator;

        public GeneratorServiceTests()
        {
            _mockWinget = new Mock<IPackageManager>();
            _mockSettings = new Mock<ISystemSettingsService>();
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockLogger = new Mock<ILogger>();

            var managers = new Dictionary<string, IPackageManager> { { "winget", _mockWinget.Object } };

            _generator = new GeneratorService(
                managers,
                _mockSettings.Object,
                _mockProcessRunner.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GenerateAsync_Captures_SystemSettings()
        {
            // Arrange
            var capturedSettings = new Dictionary<string, object>
            {
                { "dark_mode", true },
                { "taskbar_alignment", "center" }
            };

            _mockSettings.Setup(s => s.GetCapturedSettingsAsync())
                         .ReturnsAsync(capturedSettings);

            // Act
            var config = await _generator.GenerateAsync();

            // Assert
            Assert.NotNull(config.SystemSettings);
            Assert.True((bool)config.SystemSettings["dark_mode"]);
            Assert.Equal("center", config.SystemSettings["taskbar_alignment"]);
        }

        [Fact]
        public async Task GenerateAsync_Captures_GitConfig()
        {
            // Arrange
            _mockSettings.Setup(s => s.GetCapturedSettingsAsync())
                         .ReturnsAsync(new Dictionary<string, object>());

            _mockProcessRunner.Setup(r => r.RunAndCapture("git", "config --global user.name"))
                              .Returns("Test User");
            _mockProcessRunner.Setup(r => r.RunAndCapture("git", "config --global user.email"))
                              .Returns("test@example.com");

            // Act
            var config = await _generator.GenerateAsync();

            // Assert
            Assert.NotNull(config.Git);
            Assert.Equal("Test User", config.Git.UserName);
            Assert.Equal("test@example.com", config.Git.UserEmail);
        }

        [Fact]
        public void ParseWingetExport_Parses_Json_Correctly()
        {
            // Arrange
            string json = @"{
              ""$schema"": ""https://aka.ms/winget-packages.schema.2.0.json"",
              ""CreationDate"": ""2021-01-01T00:00:00.000-00:00"",
              ""Sources"": [
                {
                  ""Packages"": [
                    {
                      ""PackageIdentifier"": ""Microsoft.PowerToys""
                    },
                    {
                      ""PackageIdentifier"": ""Mozilla.Firefox""
                    }
                  ],
                  ""SourceDetails"": {
                    ""Argument"": ""https://cdn.winget.microsoft.com/cache"",
                    ""Identifier"": ""Microsoft.Winget.Source_8wekyb3d8bbwe"",
                    ""Name"": ""winget"",
                    ""Type"": ""Microsoft.Winget.Source""
                  }
                }
              ],
              ""WinGetVersion"": ""1.0.0""
            }";

            // Act
            var apps = GeneratorService.ParseWingetExport(json);

            // Assert
            Assert.Equal(2, apps.Count);
            Assert.Contains(apps, a => a.Id == "Microsoft.PowerToys" && a.Manager == "winget");
            Assert.Contains(apps, a => a.Id == "Mozilla.Firefox" && a.Manager == "winget");
        }
        [Fact]
        public void ParseScoopList_Parses_ValidPackages()
        {
            string output = """
Name       Version Source Updated Info
----       ------- ------ ------- ----
git        2.45.1  main
nodejs     20.11.0 main
""";

            var apps = GeneratorService.ParseScoopList(output);

            Assert.Equal(2, apps.Count);
            Assert.Contains(apps, a => a.Id == "git" && a.Manager == "scoop");
            Assert.Contains(apps, a => a.Id == "nodejs" && a.Manager == "scoop");
        }

        [Fact]
        public void ParseScoopList_Ignores_EmptyAndWarningOutput()
        {
            string output = """
WARN Scoop is not installed properly
error something failed
""";

            var apps = GeneratorService.ParseScoopList(output);

            Assert.Empty(apps);
        }

        [Fact]
        public void ParseChocolateyList_Parses_ValidPackages()
        {
            string output = """
Chocolatey v2.4.3
git 2.45.1
nodejs 20.11.0
2 packages installed.
""";

            var apps = GeneratorService.ParseChocolateyList(output);

            Assert.Equal(2, apps.Count);
            Assert.Contains(apps, a => a.Id == "git" && a.Manager == "chocolatey");
            Assert.Contains(apps, a => a.Id == "nodejs" && a.Manager == "chocolatey");
        }

        [Fact]
        public void ParseChocolateyList_Ignores_WarningsAndUpdateMessages()
        {
            string output = """
Chocolatey v2.4.3
A newer version of Chocolatey is available.
Use choco upgrade chocolatey to upgrade.
Warnings:
10 packages installed.
""";

            var apps = GeneratorService.ParseChocolateyList(output);

            Assert.Empty(apps);
        }


    }
}
