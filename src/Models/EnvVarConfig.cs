using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class EnvVarConfig
    {
        [YamlMember(Alias = "variable")]
        public string Variable { get; set; } = string.Empty;

        [YamlMember(Alias = "value")]
        public string Value { get; set; } = string.Empty;

        [YamlMember(Alias = "action")]
        public string Action { get; set; } = "set"; 
    }
}
