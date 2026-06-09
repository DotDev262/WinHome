using System.Text.Json.Serialization;

namespace WinHome.Models
{
  /// <summary>Deserialized model for a GitHub release from the API.</summary>
  public class GitHubRelease
  {
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; set; } = new();

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
  }

  /// <summary>Describes an asset attached to a GitHub release.</summary>
  public class GitHubAsset
  {
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
  }
}
