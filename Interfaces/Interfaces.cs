using WinHome.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinHome.Interfaces
{
    public interface IPackageManager
    {
        bool IsAvailable();
        void Install(AppConfig app, bool dryRun);
        void Uninstall(string appId, bool dryRun);
        bool IsInstalled(string appId);
    }

    public interface IDotfileService
    {
        void Apply(DotfileConfig dotfile, bool dryRun);
    }

    public interface IRegistryService
    {
        void Apply(RegistryTweak tweak, bool dryRun);
        void Revert(string path, string name, bool dryRun);
    }

    public interface ISystemSettingsService
    {
        Task<IEnumerable<RegistryTweak>> GetTweaksAsync(Dictionary<string, object> settings);
    }

    public interface IWslService
    {
        void Configure(WslConfig config, bool dryRun);
    }

    public interface IGitService
    {
        void Configure(GitConfig config, bool dryRun);
    }

    public interface IEnvironmentService
    {
        void Apply(EnvVarConfig env, bool dryRun);
    }
}
