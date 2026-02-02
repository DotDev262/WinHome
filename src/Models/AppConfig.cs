using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class AppConfig
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = string.Empty;

        [YamlMember(Alias = "source")]
        public string? Source { get; set; }

        [YamlMember(Alias = "manager")]
        public string Manager { get; set; } = "winget";

        [YamlMember(Alias = "version")]
        public string? Version { get; set; }

        [YamlMember(Alias = "params")]
        public string? Params { get; set; }
    }
}
