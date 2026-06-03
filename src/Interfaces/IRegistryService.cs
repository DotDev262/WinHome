using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Service for applying and reverting Windows Registry tweaks.</summary>
  public interface IRegistryService
  {
    /// <summary>Applies the given registry tweak.</summary>
    /// <returns><c>true</c> if the tweak was applied successfully.</returns>
    bool Apply(RegistryTweak tweak, bool dryRun);
    /// <summary>Reverts a registry value to its original state.</summary>
    /// <param name="path">Full registry key path.</param>
    /// <param name="name">Value name to revert.</param>
    /// <param name="dryRun">If <c>true</c>, simulates without making changes.</param>
    /// <returns><c>true</c> if the reversion was successful.</returns>
    bool Revert(string path, string name, bool dryRun);
    /// <summary>Reads a registry value.</summary>
    /// <returns>The value, or <c>null</c> if not found.</returns>
    object? Read(string path, string name);
  }
}
