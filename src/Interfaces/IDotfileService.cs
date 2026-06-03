using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Service for applying dotfile configurations (symlinks, copies, etc.).</summary>
  public interface IDotfileService
  {
    /// <summary>Applies the given dotfile configuration.</summary>
    /// <param name="dotfile">Dotfile configuration to apply.</param>
    /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
    void Apply(DotfileConfig dotfile, bool dryRun);
  }
}
