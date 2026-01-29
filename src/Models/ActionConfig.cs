using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class ActionConfig
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "arguments")]
        public string? Arguments { get; set; }

        [YamlMember(Alias = "workingDirectory")]
        public string? WorkingDirectory { get; set; }
    }
}
