using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
  /// <summary>Bootstraps the Bun JavaScript/TypeScript runtime via Scoop.</summary>
  public class BunBootstrapper : IPackageManagerBootstrapper
  {
    private readonly IProcessRunner _processRunner;
    public string Name => "bun";

    /// <summary>Initializes a new instance of <see cref="BunBootstrapper"/>.</summary>
    public BunBootstrapper(IProcessRunner processRunner)
    {
      _processRunner = processRunner;
    }

    /// <summary>Returns <c>true</c> if Bun is available on the system.</summary>
    public bool IsInstalled()
    {
      return _processRunner.RunCommand("bun", new[] { "--version" }, false);
    }

    /// <summary>Installs Bun via Scoop.</summary>
    public void Install(bool dryRun)
    {
      if (dryRun)
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DryRun] Would install {Name} (JS/TS Runtime)");
        Console.ResetColor();
        return;
      }

      Console.WriteLine($"[Bootstrapper] Installing {Name} via Scoop...");

      string scoopPath = GetScoopPath();

      var psi = new ProcessStartInfo
      {
        FileName = scoopPath,
        Arguments = "install bun",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      try
      {
        _processRunner.RunProcessWithStartInfo(psi);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Bootstrapper] Error installing {Name}: {ex.Message}");
        throw;
      }

      Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
    }

    private string GetScoopPath()
    {
      string scoopPath = "scoop.cmd";
      string userScoop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
      string globalScoop = Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? "C:\\ProgramData", "scoop", "shims", "scoop.cmd");

      if (File.Exists(userScoop)) scoopPath = userScoop;
      else if (File.Exists(globalScoop)) scoopPath = globalScoop;

      return scoopPath;
    }
  }
}
