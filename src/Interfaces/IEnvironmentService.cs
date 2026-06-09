using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Service for managing Windows environment variables.</summary>
  public interface IEnvironmentService
  {
    /// <summary>Applies the given environment variable configuration.</summary>
    /// <param name="env">Environment variable configuration to apply.</param>
    /// <param name="dryRun">If <c>true</c>, simulates the operation without making changes.</param>
    void Apply(EnvVarConfig env, bool dryRun);
    /// <summary>Refreshes the current process PATH environment variable from the system.</summary>
    void RefreshPath();
  }
}
