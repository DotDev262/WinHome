using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace WinHome.Models
{
     public class ProfileConfig
    {
        [YamlMember(Alias = "git")]
        [JsonPropertyName("git")]
        public GitConfig? Git { get; set; }
        
        // You can add 'Apps' or 'SystemSettings' here later if you want profile-specific apps
    }
}
