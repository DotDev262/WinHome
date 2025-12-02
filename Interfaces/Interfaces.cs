using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IPackageManager
    {
        // Every manager must provide these three capabilities
        bool IsAvailable(); // Is Chocolatey/Winget actually installed on this PC?
        void Install(AppConfig app);
        void Uninstall(string appId);
        bool IsInstalled(string appId);
    }
}