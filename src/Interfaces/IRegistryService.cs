using WinHome.Models;

namespace WinHome.Interfaces
{
    public interface IRegistryService
    {
        void Apply(RegistryTweak tweak, bool dryRun);
        void Revert(string path, string name, bool dryRun);
        object? Read(string path, string name);
    }
}
