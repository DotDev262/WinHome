using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class WslDistroConfig
    {
        [YamlMember(Alias = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; 

        [YamlMember(Alias = "setupScript")]
        [JsonPropertyName("setupScript")]
        public string? SetupScript { get; set; }
    }
}
