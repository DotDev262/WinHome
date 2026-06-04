using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Service for configuring Git settings (user name, email, etc.).</summary>
  public interface IGitService
  {
    /// <summary>Applies the given Git configuration.</summary>
    /// <param name="config">Git configuration to apply.</param>
    /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
    void Configure(GitConfig config, bool dryRun);
  }
}
