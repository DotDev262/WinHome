using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Bootstrappers;

namespace WinHome.Services.Managers
{
  /// <summary>Manages package operations via Scoop.</summary>
  public class ScoopService : IPackageManager
  {
    private readonly IProcessRunner _processRunner;
    private readonly ILogger _logger;
    private readonly IRuntimeResolver _resolver;
    public IPackageManagerBootstrapper Bootstrapper { get; }

    /// <summary>Initializes a new instance of <see cref="ScoopService"/>.</summary>
    public ScoopService(IProcessRunner processRunner, IPackageManagerBootstrapper bootstrapper, ILogger logger, IRuntimeResolver resolver)
    {
      _processRunner = processRunner;
      Bootstrapper = bootstrapper;
      _logger = logger;
      _resolver = resolver;
    }

    private string GetScoopExecutable()
    {
      return _resolver.Resolve("scoop");
    }

    /// <summary>Returns <c>true</c> if Scoop is available on the system.</summary>
    public bool IsAvailable()
    {
      return Bootstrapper.IsInstalled();
    }

    /// <summary>Installs a package via Scoop. Skips if already installed.</summary>
    public void Install(AppConfig app, bool dryRun)
    {
      string executable = GetScoopExecutable();
      if (IsInstalled(app.Id))
      {
        _logger.LogInfo($"[Scoop] {app.Id} is already installed.");
        return;
      }

      if (dryRun)
      {
        _logger.LogWarning($"[DryRun] Would install '{app.Id}' via Scoop");
        return;
      }

      _logger.LogInfo($"[Scoop] Installing {app.Id}...");
      var args = new[] { "install", app.Id };

      bool alreadyInstalled = false;
      bool manifestNotFound = false;
      bool success = _processRunner.RunCommand(executable, args, false, line =>
      {
        if (line == null) return;
        _logger.LogInfo($"[Scoop:Install] {line}");
        if (line.Contains($"'{app.Id}' is already installed", StringComparison.OrdinalIgnoreCase))
        {
          alreadyInstalled = true;
        }
        if (line.Contains("Couldn't find manifest", StringComparison.OrdinalIgnoreCase))
        {
          manifestNotFound = true;
        }
      });

      if (!success || manifestNotFound)
      {
        if (alreadyInstalled)
        {
          _logger.LogSuccess($"[Success] {app.Id} is already installed (detected during install attempt).");
          return;
        }
        throw new Exception($"Failed to install {app.Id} using Scoop.{(manifestNotFound ? " Manifest not found." : "")}");
      }
      _logger.LogSuccess($"[Success] Installed {app.Id}");
    }

    /// <summary>Uninstalls a package via Scoop.</summary>
    public void Uninstall(string appId, bool dryRun)
    {
      string executable = GetScoopExecutable();
      if (dryRun)
      {
        _logger.LogWarning($"[DryRun] Would uninstall '{appId}' via Scoop");
        return;
      }

      _logger.LogInfo($"[Scoop] Uninstalling {appId}...");
      var args = new[] { "uninstall", appId };

      if (!_processRunner.RunCommand(executable, args, false, line => _logger.LogInfo($"[Scoop:Uninstall] {line}")))
      {
        throw new Exception($"Failed to uninstall {appId} using Scoop.");
      }
      _logger.LogSuccess($"[Success] Uninstalled {appId}");
    }

    /// <summary>Checks whether a package is installed via Scoop.</summary>
    public bool IsInstalled(string appId)
    {
      string executable = GetScoopExecutable();
      string output = _processRunner.RunCommandWithOutput(executable, new[] { "list" });
      return output.Contains(appId, StringComparison.OrdinalIgnoreCase);
    }
  }
}
