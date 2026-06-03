namespace WinHome.Interfaces
{
  /// <summary>Bootstraps a package manager by installing it if not already present on the system.</summary>
  public interface IPackageManagerBootstrapper
  {
    /// <summary>Gets the display name of the package manager (e.g. "winget", "scoop").</summary>
    string Name { get; }
    /// <summary>Returns <c>true</c> if the package manager is already installed.</summary>
    bool IsInstalled();
    /// <summary>Installs the package manager.</summary>
    /// <param name="dryRun">If <c>true</c>, simulates the installation without making changes.</param>
    void Install(bool dryRun);
  }
}
