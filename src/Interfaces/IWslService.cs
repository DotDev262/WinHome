using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Service for configuring Windows Subsystem for Linux (WSL) instances and distros.</summary>
  public interface IWslService
  {
    /// <summary>Applies the given WSL configuration.</summary>
    /// <param name="config">WSL configuration to apply.</param>
    /// <param name="dryRun">If <c>true</c>, simulates without making changes.</param>
    void Configure(WslConfig config, bool dryRun);
  }
}
