using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IPackageManager
    {
        IPackageManagerBootstrapper Bootstrapper { get; }
        bool IsAvailable();
        void Install(AppConfig app, bool dryRun);
        void Uninstall(string appId, bool dryRun);
        bool IsInstalled(string appId);
    }
}
