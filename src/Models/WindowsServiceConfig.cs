using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class WindowsServiceConfig
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        [YamlMember(Alias = "state")]
        public string State { get; set; } = "running"; // running, stopped

        [YamlMember(Alias = "startup")]
        public string? StartupType { get; set; } // automatic, manual, disabled
    }
}
