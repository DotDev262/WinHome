using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
  /// <summary>Bootstraps Chocolatey package manager using the official PowerShell install script.</summary>
  public class ChocolateyBootstrapper : IPackageManagerBootstrapper
  {
    private readonly IProcessRunner _processRunner;
    private const int MaxRetries = 3;
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

    /// <summary>Installs Chocolatey via the community PowerShell install script. Retries on network errors with max attempt limit.</summary>
    public void Install(bool dryRun)
    {
      if (dryRun)
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DryRun] Would install {Name}");
        Console.ResetColor();
        return;
      }

      string command = "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; " +
                       "iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))";

      for (int attempt = 0; attempt < MaxRetries; attempt++)
      {
        Console.WriteLine($"[Bootstrapper] Installing {Name} (attempt {attempt + 1}/{MaxRetries})...");

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
          Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
          break;
        }
        catch (Exception ex) when (
          ex.Message.Contains("remote name could not be resolved") ||
          ex.Message.Contains("Operation timed out"))
        {
          if (attempt < MaxRetries - 1)
          {
            Console.WriteLine($"[Bootstrapper] Network error installing {Name}. Retrying in 10 seconds...");
            Thread.Sleep(10000);
          }
          else
          {
            throw new Exception($"Failed to install {Name} after {MaxRetries} attempts: {ex.Message}", ex);
          }
        }
        catch (Exception ex)
        {
          throw new Exception($"Failed to install {Name}: {ex.Message}", ex);
        }
      }

      Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
      // Issue #392 Fix: Refresh the environment PATH for the current process so it can see the newly installed manager
      if (OperatingSystem.IsWindows())
      {
        string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
        string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
        string newPath = $"{machinePath};{userPath}";
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Process);
      }

    }
  }
}
