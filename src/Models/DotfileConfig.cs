using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class DotfileConfig
    {
        [YamlMember(Alias = "src")]
        [JsonPropertyName("src")]
        public string Src { get; set; } = string.Empty;

        [YamlMember(Alias = "target")]
        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;
    }
}
