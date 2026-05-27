using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.System;

namespace WinHome.Tests.Services.System
{
  public class UpdateServiceTests
  {
    private readonly Mock<ILogger> _mockLogger;

    public UpdateServiceTests()
    {
      _mockLogger = new Mock<ILogger>();
    }

    private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
      var handlerMock = new Mock<HttpMessageHandler>();
      handlerMock.Protected()
          .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage
          {
            StatusCode = statusCode,
            Content = new StringContent(responseContent)
          });
      return handlerMock;
    }

    private Mock<HttpMessageHandler> CreateMockHttpMessageHandlerThrows(Exception exception)
    {
      var handlerMock = new Mock<HttpMessageHandler>();
      handlerMock.Protected()
          .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>())
          .ThrowsAsync(exception);
      return handlerMock;
    }

    #region CheckForUpdatesAsync Tests

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNewerVersionAvailable_ReturnsTrue()
    {
      // Arrange
      var releaseJson = JsonSerializer.Serialize(new GitHubRelease { TagName = "v2.0.0" });
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, releaseJson);
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.True(result);
      _mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("New version available"))), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenSameVersion_ReturnsFalse()
    {
      // Arrange
      var releaseJson = JsonSerializer.Serialize(new GitHubRelease { TagName = "v1.0.0" });
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, releaseJson);
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.False(result);
      _mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("latest version"))), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenCurrentVersionIsNewer_ReturnsFalse()
    {
      // Arrange
      var releaseJson = JsonSerializer.Serialize(new GitHubRelease { TagName = "v0.9.0" });
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, releaseJson);
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.False(result);
      _mockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("latest version"))), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenApiReturnsNull_ReturnsFalse()
    {
      // Arrange
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, "null");
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.False(result);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenPreReleaseVersionFallback_ReturnsTrue()
    {
      // Arrange
      var releaseJson = JsonSerializer.Serialize(new GitHubRelease { TagName = "v2.0.0-rc1" });
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, releaseJson);
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.True(result);
      _mockLogger.Verify(l => l.LogSuccess(It.Is<string>(s => s.Contains("New version available"))), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNetworkError_ReturnsFalseAndLogsWarning()
    {
      // Arrange
      var handlerMock = CreateMockHttpMessageHandlerThrows(new HttpRequestException("Network down"));
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.False(result);
      _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Failed to check for updates"))), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenApiReturns404_ReturnsFalseAndLogsWarning()
    {
      // Arrange
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.NotFound, "");
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      var result = await service.CheckForUpdatesAsync("1.0.0");

      // Assert
      Assert.False(result);
      _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Failed to check for updates"))), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenReleaseInfoIsNull_LogsErrorAndAborts()
    {
      // Arrange
      // Return empty JSON or unparseable to simulate null release
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, "null");
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      await service.UpdateAsync();

      // Assert
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Could not fetch release info"))), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAssetNotFound_LogsErrorAndAborts()
    {
      // Arrange
      var release = new GitHubRelease
      {
        TagName = "v2.0.0",
        Assets = new List<GitHubAsset>
                {
                    new GitHubAsset { Name = "OtherFile.zip", BrowserDownloadUrl = "http://example.com/OtherFile.zip" }
                }
      };
      var releaseJson = JsonSerializer.Serialize(release);
      var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, releaseJson);
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      await service.UpdateAsync();

      // Assert
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Could not find 'WinHome.exe'"))), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNetworkFails_LogsError()
    {
      // Arrange
      var handlerMock = CreateMockHttpMessageHandlerThrows(new HttpRequestException("Failed API call"));
      using var httpClient = new HttpClient(handlerMock.Object);

      var service = new UpdateService(_mockLogger.Object, httpClient);

      // Act
      await service.UpdateAsync();

      // Assert
      _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Update failed: Failed API call"))), Times.Once);
    }

    #endregion

    #region Deserialization Tests

    [Fact]
    public void GitHubRelease_DeserializesCorrectly()
    {
      // Arrange
      string json = @"
            {
                ""tag_name"": ""v1.5.0"",
                ""name"": ""Release 1.5"",
                ""body"": ""Changelog..."",
                ""assets"": [
                    {
                        ""name"": ""WinHome.exe"",
                        ""browser_download_url"": ""https://github.com/DotDev262/WinHome/releases/download/v1.5.0/WinHome.exe""
                    }
                ]
            }";

      // Act
      var release = JsonSerializer.Deserialize<GitHubRelease>(json);

      // Assert
      Assert.NotNull(release);
      Assert.Equal("v1.5.0", release.TagName);
      Assert.Equal("Release 1.5", release.Name);
      Assert.Equal("Changelog...", release.Body);
      Assert.Single(release.Assets);
      Assert.Equal("WinHome.exe", release.Assets[0].Name);
      Assert.Equal("https://github.com/DotDev262/WinHome/releases/download/v1.5.0/WinHome.exe", release.Assets[0].BrowserDownloadUrl);
    }

    [Fact]
    public void GitHubAsset_DeserializesCorrectly()
    {
      // Arrange
      string json = @"
            {
                ""name"": ""WinHome.exe"",
                ""browser_download_url"": ""https://example.com/download""
            }";

      // Act
      var asset = JsonSerializer.Deserialize<GitHubAsset>(json);

      // Assert
      Assert.NotNull(asset);
      Assert.Equal("WinHome.exe", asset.Name);
      Assert.Equal("https://example.com/download", asset.BrowserDownloadUrl);
    }

    #endregion
  }
}
