using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Manages Windows service configurations (startup type, state, etc.).</summary>
  public interface IWindowsServiceManager
  {
    /// <summary>Applies Windows service configuration changes.</summary>
    /// <param name="service">Windows service configuration.</param>
    /// <param name="dryRun">If <c>true</c>, simulates without making changes.</param>
    void Apply(WindowsServiceConfig service, bool dryRun);
  }
}
