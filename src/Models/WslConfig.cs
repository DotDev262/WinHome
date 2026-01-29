using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class WslConfig
    {
        [YamlMember(Alias = "defaultVersion")]
        public int DefaultVersion { get; set; } = 2;

        [YamlMember(Alias = "defaultDistro")]
        public string? DefaultDistro { get; set; }

        [YamlMember(Alias = "update")]
        public bool Update { get; set; } = false;

        
        [YamlMember(Alias = "distros")]
        public List<WslDistroConfig> Distros { get; set; } = new();
    }
}
