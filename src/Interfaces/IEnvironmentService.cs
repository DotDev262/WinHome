using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IEnvironmentService
    {
        void Apply(EnvVarConfig env, bool dryRun);
    }
}
