using WinHome.Models;

namespace WinHome.Interfaces
{
  public interface IRegistryService
  {
    bool Apply(RegistryTweak tweak, bool dryRun);
    bool Revert(string path, string name, bool dryRun);
    object? Read(string path, string name);
  }
}
