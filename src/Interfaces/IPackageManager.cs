using WinHome.Models;

namespace WinHome.Interfaces
{
  /// <summary>Abstraction for a package manager (winget, scoop, chocolatey, etc.).</summary>
  public interface IPackageManager
  {
    /// <summary>Gets the bootstrapper used to install this package manager if missing.</summary>
    IPackageManagerBootstrapper Bootstrapper { get; }
    /// <summary>Returns <c>true</c> if the package manager is available on the system.</summary>
    bool IsAvailable();
    /// <summary>Installs the specified application package.</summary>
    void Install(AppConfig app, bool dryRun);
    /// <summary>Uninstalls the application with the given package ID.</summary>
    void Uninstall(string appId, bool dryRun);
    /// <summary>Returns <c>true</c> if the application with the given ID is installed.</summary>
    bool IsInstalled(string appId);
  }
}
