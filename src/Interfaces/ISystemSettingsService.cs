using WinHome.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinHome.Interfaces
{
    public interface ISystemSettingsService
    {
        Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object> settings);
        Task ApplyNonRegistrySettingsAsync(Dictionary<string, object> settings, bool dryRun);
    }
}
