using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
  /// <summary>Bootstraps Chocolatey package manager using the official PowerShell install script.</summary>
  public class ChocolateyBootstrapper : IPackageManagerBootstrapper
  {
    private readonly IProcessRunner _processRunner;
    public string Name => "Chocolatey";

    /// <summary>Initializes a new instance of <see cref="ChocolateyBootstrapper"/>.</summary>
    public ChocolateyBootstrapper(IProcessRunner processRunner)
    {
      _processRunner = processRunner;
    }

    /// <summary>Returns <c>true</c> if Chocolatey is installed on the system.</summary>
    public bool IsInstalled()
    {
      if (_processRunner.RunCommand("choco", new[] { "--version" }, false)) return true;

      string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe");
      return File.Exists(chocoPath);
    }

    /// <summary>Installs Chocolatey via the community PowerShell install script. Retries on network errors.</summary>
    public void Install(bool dryRun)
    {
      if (dryRun)
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DryRun] Would install {Name}");
        Console.ResetColor();
        return;
      }

      Console.WriteLine($"[Bootstrapper] Installing {Name}...");

      string command = "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; " +
                       "iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))";

      var psi = new ProcessStartInfo
      {
        FileName = "powershell.exe",
        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
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
        if (ex.Message.Contains("remote name could not be resolved") || ex.Message.Contains("Operation timed out"))
        {
          Console.WriteLine($"[Bootstrapper] Network error installing {Name}. Retrying in 10 seconds...");
          Thread.Sleep(10000);
          Install(false);
          return;
        }
        if (!ex.Message.Contains("Process failed with exit code"))
        {
          Console.WriteLine($"[Bootstrapper] Unexpected error: {ex.Message}. Retrying...");
          Thread.Sleep(5000);
          Install(false);
          return;
        }
        throw new Exception($"Failed to install {Name}: {ex.Message}", ex);
      }

      Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
    }
  }
}
