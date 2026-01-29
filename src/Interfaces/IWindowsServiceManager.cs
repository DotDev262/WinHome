using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IWindowsServiceManager
    {
        void Apply(WindowsServiceConfig service, bool dryRun);
    }
}
