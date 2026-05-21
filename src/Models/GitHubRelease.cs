using System.Text.Json.Serialization;

namespace WinHome.Models
{
    /// <summary>
    /// Represents the metadata of a released version fetched from the GitHub API.
    /// </summary>
    public class GitHubRelease
    {
        /// <summary>
        /// Gets or sets the tag name associated with the release (e.g., "v1.0.0").
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the descriptive title name of the release.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of distribution assets or binaries included in the release.
        /// </summary>
        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();

        /// <summary>
        /// Gets or sets the markdown text body describing the release notes or changelog.
        /// </summary>
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an individual downloadable file asset bundled within a GitHub release.
    /// </summary>
    public class GitHubAsset
    {
        /// <summary>
        /// Gets or sets the filename of the asset.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the remote direct download URL browser link for the asset.
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}