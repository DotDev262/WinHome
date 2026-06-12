using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WinHome.Interfaces;
using WinHome.Models.Plugins;

namespace WinHome.Services.Plugins
{
  /// <summary>Executes plugin commands by spawning a child process and communicating via stdin/stdout JSON messages.</summary>
  public class PluginRunner : IPluginRunner
  {
    private readonly ILogger _logger;
    private readonly IRuntimeResolver _runtimeResolver;

    /// <summary>Initializes a new instance of <see cref="PluginRunner"/>.</summary>
    public PluginRunner(ILogger logger, IRuntimeResolver runtimeResolver)
    {
      _logger = logger;
      _runtimeResolver = runtimeResolver;
    }

    /// <summary>Executes a plugin command: sends a JSON request via stdin and reads the JSON response from stdout.</summary>
    /// <param name="plugin">The plugin manifest to execute.</param>
    /// <param name="command">The command name to run.</param>
    /// <param name="args">Optional arguments for the command.</param>
    /// <param name="context">Optional execution context.</param>
    /// <param name="timeout">Optional timeout (default 30s, minimum 1s).</param>
    /// <returns>A <see cref="PluginResult"/> from the plugin's response.</returns>
    public async Task<PluginResult> ExecuteAsync(PluginManifest plugin, string command, object? args, object? context, TimeSpan? timeout = null)
    {
      var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
      if (actualTimeout < TimeSpan.FromSeconds(1)) actualTimeout = TimeSpan.FromSeconds(1);

      var (fileName, argumentList) = BuildProcessStartInfo(plugin.Type, Path.Combine(plugin.DirectoryPath, plugin.Main));

      var request = new PluginRequest
      {
        Command = command,
        Args = args,
        Context = context
      };

      var startInfo = new ProcessStartInfo
      {
        FileName = fileName,
        WorkingDirectory = plugin.DirectoryPath,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      foreach (var arg in argumentList)
      {
        startInfo.ArgumentList.Add(arg);
      }

      // Inject Environment Variables if needed
      startInfo.Environment["WINHOME_PLUGIN_NAME"] = plugin.Name;

      using var process = new Process { StartInfo = startInfo };
      using var cts = new CancellationTokenSource(actualTimeout);
      var sw = Stopwatch.StartNew();

      try
      {
        _logger.LogInfo($"[PluginRunner] Starting {plugin.Name}: {fileName} {string.Join(" ", argumentList)}");
        if (!process.Start())
        {
          return new PluginResult { Success = false, Error = "Failed to start plugin process." };
        }

        // Handling Stderr asynchronously to avoid deadlocks and log immediately
        process.ErrorDataReceived += (sender, e) =>
        {
          if (!string.IsNullOrEmpty(e.Data)) _logger.LogWarning($"[{plugin.Name}][STDERR] {e.Data}");
        };
        process.BeginErrorReadLine();

        // 1. Send Request
        try
        {
          string jsonRequest = JsonSerializer.Serialize(request);
          await process.StandardInput.WriteLineAsync(jsonRequest.AsMemory(), cts.Token);
          process.StandardInput.Close(); // Close Stdin to signal EOF to the plugin if it reads until EOF
        }
        catch (IOException) when (process.HasExited)
        {
          // Ignore pipe errors if the process has already exited (e.g. simple echo scripts)
        }

        // 2. Read Response with Size Limit (10MB) and Timeout
        var outputBuilder = new StringBuilder();
        char[] buffer = new char[4096];
        int totalRead = 0;
        int maxBytes = 10 * 1024 * 1024; // 10 MB

        while (true)
        {
          int read = await process.StandardOutput.ReadAsync(buffer.AsMemory(), cts.Token);
          if (read == 0) break;

          totalRead += read;
          if (totalRead > maxBytes)
          {
            process.Kill();
            throw new InvalidOperationException("Plugin output exceeded size limit (10MB).");
          }

          outputBuilder.Append(buffer, 0, read);
        }

        await process.WaitForExitAsync(cts.Token);
        sw.Stop();

        if (process.ExitCode != 0)
        {
          return new PluginResult { Success = false, Error = $"Plugin process exited with code {process.ExitCode}" };
        }

        string output = outputBuilder.ToString();

        if (string.IsNullOrWhiteSpace(output))
        {
          return new PluginResult { Success = false, Error = "Plugin returned empty response." };
        }

        try
        {
          // Attempt robust parsing: Find the last line that looks like JSON
          var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
          string jsonLine = lines.LastOrDefault(l => l.StartsWith("{") && l.EndsWith("}")) ?? output;

          var result = JsonSerializer.Deserialize<PluginResult>(jsonLine);
          return result ?? new PluginResult { Success = false, Error = "Deserialized null result." };
        }
        catch (JsonException ex)
        {
          return new PluginResult { Success = false, Error = $"Invalid JSON response: {ex.Message}. Output: {output}" };
        }
      }
      catch (OperationCanceledException)
      {
        sw.Stop();
        try
        {
          if (!process.HasExited)
          {
            process.Kill(entireProcessTree: true);
          }
        }
        catch (Exception killEx)
        {
          _logger.LogWarning($"[PluginRunner] Failed to kill plugin process tree for {plugin.Name}: {killEx.Message}");
        }
        _logger.LogWarning($"[PluginRunner] Plugin {plugin.Name} timed out and was killed after {sw.ElapsedMilliseconds}ms.");
        return new PluginResult { Success = false, Error = $"Plugin timed out after {actualTimeout.TotalSeconds:F0}s." };
      }
      catch (Exception ex)
      {
        sw.Stop();
        try
        {
          if (!process.HasExited)
          {
            process.Kill(entireProcessTree: true);
          }
        }
        catch (Exception killEx)
        {
          _logger.LogWarning($"[PluginRunner] Failed to kill plugin process tree for {plugin.Name}: {killEx.Message}");
        }
        return new PluginResult { Success = false, Error = $"Runner Exception: {ex.Message}" };
      }
    }

    /// <summary>Builds the process filename and arguments for executing a plugin based on its type.</summary>
    public (string FileName, IList<string> ArgumentList) BuildProcessStartInfo(string runnerType, string mainPath)
    {
      switch (runnerType.ToLowerInvariant())
      {
        case "python":
          var uvPath = _runtimeResolver.Resolve("uv");
          return (uvPath, new List<string> { "run", "--quiet", mainPath });

        case "typescript":
        case "javascript":
          var bunPath = _runtimeResolver.Resolve("bun");
          return (bunPath, new List<string> { "run", mainPath });

        case "executable":
          return (mainPath, new List<string>());

        case "powershell":
          string powershellPath = "powershell";
          try
          {
            var pwshResolved = _runtimeResolver.Resolve("pwsh");
            if (pwshResolved != "pwsh")
            {
              powershellPath = pwshResolved;
            }
            else
            {
              powershellPath = _runtimeResolver.Resolve("powershell");
            }
          }
          catch
          {
            powershellPath = _runtimeResolver.Resolve("powershell");
          }
          return (powershellPath, new List<string> { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", mainPath });

        default:
          throw new NotSupportedException($"Runner type {runnerType} is not supported.");
      }
    }
  }
}
