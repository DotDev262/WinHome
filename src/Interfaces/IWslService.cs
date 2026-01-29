using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IWslService
    {
        void Configure(WslConfig config, bool dryRun);
    }
}
