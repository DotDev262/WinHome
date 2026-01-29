using YamlDotNet.Serialization;

namespace WinHome.Models
{
    public class WslDistroConfig
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty; 

        [YamlMember(Alias = "setupScript")]
        public string? SetupScript { get; set; }
    }
}
