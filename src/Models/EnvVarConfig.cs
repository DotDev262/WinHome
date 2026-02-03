using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class EnvVarConfig
    {
        [YamlMember(Alias = "variable")]
        [JsonPropertyName("variable")]
        public string Variable { get; set; } = string.Empty;

        [YamlMember(Alias = "value")]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [YamlMember(Alias = "action")]
        [JsonPropertyName("action")]
        public string Action { get; set; } = "set"; 
    }
}
