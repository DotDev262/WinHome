using WinHome.Models;

namespace WinHome.Interfaces
{
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
        IEnumerable<RegistryTweak> GetTweaks(Dictionary<string, object> settings);
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