using System.Diagnostics;
using WinHome.Interfaces;

namespace WinHome.Services.Bootstrappers
{
  /// <summary>Bootstraps the Scoop package manager using the official install script.</summary>
  public class ScoopBootstrapper : IPackageManagerBootstrapper
  {
    private readonly IProcessRunner _processRunner;
    private const int MaxRetries = 3;
    public string Name => "Scoop";

    /// <summary>Initializes a new instance of <see cref="ScoopBootstrapper"/>.</summary>
    public ScoopBootstrapper(IProcessRunner processRunner)
    {
      _processRunner = processRunner;
    }

    /// <summary>Returns <c>true</c> if Scoop is installed (checks PATH and common install locations).</summary>
    public bool IsInstalled()
    {
      if (_processRunner.RunCommand("scoop", new[] { "--version" }, false)) return true;

      // Fallback for fresh installs where PATH isn't updated yet
      string[] searchPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramData") ?? @"C:\ProgramData", "scoop", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("SCOOP") ?? "", "shims", "scoop.cmd"),
                Path.Combine(Environment.GetEnvironmentVariable("SCOOP_GLOBAL") ?? "", "shims", "scoop.cmd")
            };

      foreach (var path in searchPaths)
      {
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) return true;
      }

      return false;
    }

    /// <summary>Installs Scoop via irm/get.scoop.sh. Retries on DNS errors with max attempt limit.</summary>
    public void Install(bool dryRun)
    {
      if (dryRun)
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DryRun] Would install {Name}");
        Console.ResetColor();
        return;
      }

      // Set execution policy first, then install Scoop
      // This fixes the "cannot be loaded because running scripts is disabled" error
      string command = "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force; irm get.scoop.sh -outfile install.ps1; .\\install.ps1 -RunAsAdmin; if (Test-Path .\\install.ps1) { Remove-Item .\\install.ps1 }";

      for (int attempt = 0; attempt < MaxRetries; attempt++)
      {
        Console.WriteLine($"[Bootstrapper] Installing {Name} (attempt {attempt + 1}/{MaxRetries})...");

        var psi = new ProcessStartInfo
        {
          FileName = "powershell.exe",
          Arguments = $"-NoProfile -Command \"{command}\"",
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        };

        try
        {
          _processRunner.RunProcessWithStartInfo(psi);
          Console.WriteLine($"[Bootstrapper] {Name} installed successfully.");
          return;
        }
        catch (Exception ex) when (ex.Message.Contains("remote name could not be resolved"))
        {
          if (attempt < MaxRetries - 1)
          {
            Console.WriteLine("[Bootstrapper] Network error resolving get.scoop.sh. Retrying in 10 seconds...");
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
