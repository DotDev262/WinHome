using WinHome.Interfaces;

namespace WinHome.Services.System
{
  /// <summary>Resolves the full file system path to runtime executables (git, scoop, bun, uv, etc.) via PATH and common fallback locations.</summary>
  public class RuntimeResolver : IRuntimeResolver
  {
    private readonly ILogger _logger;
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;

    /// <summary>Initializes a new instance of <see cref="RuntimeResolver"/>.</summary>
    public RuntimeResolver(ILogger logger, IProcessRunner processRunner, IFileSystem fileSystem)
    {
      _logger = logger;
      _processRunner = processRunner;
      _fileSystem = fileSystem;
    }

    public string Resolve(string runtimeName)
    {
      // 1. Check PATH and get full path
      string pathMatch = GetFullPath(runtimeName);
      if (!string.IsNullOrEmpty(pathMatch))
      {
        return pathMatch;
      }

      // 2. Common Windows paths (fallback)
      string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      if (runtimeName == "bun")
      {
        string bunPath = Path.Combine(localAppData, ".bun", "bin", "bun.exe");
        if (_fileSystem.FileExists(bunPath)) return bunPath;
      }
      else if (runtimeName == "uv")
      {
        string uvPath = Path.Combine(localAppData, "uv", "uv.exe");
        if (_fileSystem.FileExists(uvPath)) return uvPath;
      }
      else if (runtimeName == "scoop")
      {
        string scoopMainShim = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "scoop.cmd");
        if (_fileSystem.FileExists(scoopMainShim)) return scoopMainShim;
      }
      else if (runtimeName == "choco")
      {
        string chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin", "choco.exe");
        if (_fileSystem.FileExists(chocoPath)) return chocoPath;

        string chocoPathAlt = @"C:\ProgramData\chocolatey\bin\choco.exe";
        if (_fileSystem.FileExists(chocoPathAlt)) return chocoPathAlt;
      }
      else if (runtimeName == "winget")
      {
        string wingetPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", "winget.exe");
        if (_fileSystem.FileExists(wingetPath)) return wingetPath;
      }

      // 3. Scoop Shims
      string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      string scoopShim = Path.Combine(userProfile, "scoop", "shims", $"{runtimeName}.exe");
      if (_fileSystem.FileExists(scoopShim)) return scoopShim;

      string scoopCmdShim = Path.Combine(userProfile, "scoop", "shims", $"{runtimeName}.cmd");
      if (_fileSystem.FileExists(scoopCmdShim)) return scoopCmdShim;

      // Return original name and hope for the best (or it will fail and trigger bootstrapping)
      return runtimeName;
    }

    private string GetFullPath(string runtimeName)
    {
      try
      {
        var output = _processRunner.RunCommandWithOutput("where.exe", new[] { runtimeName });
        if (string.IsNullOrWhiteSpace(output)) return string.Empty;

        string fullPath = string.Empty;
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawLine in lines)
        {
          string line = rawLine.Trim();
          if (string.IsNullOrEmpty(line)) continue;
          if (!Path.IsPathRooted(line)) continue;

          if (line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
              line.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
              line.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
          {
            return line;
          }

          if (string.IsNullOrEmpty(fullPath)) fullPath = line;
        }

        if (string.IsNullOrEmpty(fullPath))
        {
          _logger.LogInfo($"[RuntimeResolver] No valid PATH match for '{runtimeName}'.");
        }

        return fullPath;
      }
      catch { return string.Empty; }
    }
  }
}
