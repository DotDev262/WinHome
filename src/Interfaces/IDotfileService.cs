using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IDotfileService
    {
        void Apply(DotfileConfig dotfile, bool dryRun);
    }
}
