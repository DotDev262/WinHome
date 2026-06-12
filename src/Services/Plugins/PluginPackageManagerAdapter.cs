using System.Diagnostics;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Models.Plugins;

namespace WinHome.Services.Plugins
{
  /// <summary>Adapts a plugin to the <see cref="IPackageManager"/> interface so plugins can install packages.</summary>
  public class PluginPackageManagerAdapter : IPackageManager
  {
    private readonly PluginManifest _plugin;
    private readonly IPluginRunner _runner;
    private readonly IPluginManager _manager;
    private readonly IRuntimeResolver _resolver;
    private readonly ILogger? _logger;

    /// <summary>Initializes a new instance of <see cref="PluginPackageManagerAdapter"/>.</summary>
    public PluginPackageManagerAdapter(PluginManifest plugin, IPluginRunner runner, IPluginManager manager, IRuntimeResolver resolver)
      : this(plugin, runner, manager, resolver, null)
    {
    }

    /// <summary>Initializes a new instance of <see cref="PluginPackageManagerAdapter"/> with a logger.</summary>
    public PluginPackageManagerAdapter(PluginManifest plugin, IPluginRunner runner, IPluginManager manager, IRuntimeResolver resolver, ILogger? logger)
    {
      ArgumentNullException.ThrowIfNull(plugin);
      ArgumentNullException.ThrowIfNull(runner);
      ArgumentNullException.ThrowIfNull(manager);
      ArgumentNullException.ThrowIfNull(resolver);

      _plugin = plugin;
      _runner = runner;
      _manager = manager;
      _resolver = resolver;
      _logger = logger;
    }

    public string PluginType => _plugin.Type;

    public IPackageManagerBootstrapper Bootstrapper => new PluginRuntimeBootstrapper(_plugin, _manager, _resolver, _logger);

    /// <summary>Returns <c>true</c> if the plugin's runtime is installed.</summary>
    public bool IsAvailable()
    {
      return Bootstrapper.IsInstalled();
    }

    /// <summary>Returns <c>true</c> if the plugin reports the package as installed.</summary>
    public bool IsInstalled(string appId)
    {
      var result = TryExecute("check_installed", new { packageId = appId }, null);
      if (result == null || result.Success != true)
        return false;
      if (result.Installed.HasValue)
        return result.Installed.Value;
      return bool.TryParse(result.Data?.ToString(), out var isInstalled) && isInstalled;
    }

    /// <summary>Installs a package by delegating to the plugin's install command.</summary>
    public void Install(AppConfig app, bool dryRun)
    {
      ArgumentNullException.ThrowIfNull(app);

      // Ensure runtime is available before execution
      EnsureRuntime();

      var args = new
      {
        packageId = app.Id,
        version = app.Version,
        @params = app.Params
      };

      var context = new { dryRun = dryRun };

      var result = ExecuteRequired("install", args, context);
      EnsureSuccess(result, "install", app.Id);
    }

    /// <summary>Uninstalls a package by delegating to the plugin's uninstall command.</summary>
    public void Uninstall(string appId, bool dryRun)
    {
      // Ensure runtime is available before execution
      EnsureRuntime();

      var context = new { dryRun = dryRun };
      var result = ExecuteRequired("uninstall", new { packageId = appId }, context);
      EnsureSuccess(result, "uninstall", appId);
    }

    private void EnsureRuntime()
    {
      _manager.EnsureRuntimeAsync(_plugin).GetAwaiter().GetResult();
    }

    private PluginResult? TryExecute(string command, object? args, object? context)
    {
      try
      {
        return _runner.ExecuteAsync(_plugin, command, args, context).GetAwaiter().GetResult();
      }
      catch (UnauthorizedAccessException ex) { _logger?.LogError($"Permissions failure during package resolution: {ex.Message}"); return null; }
      catch (global::System.Text.Json.JsonException ex) { _logger?.LogError($"Malformed registry response manifest parsing: {ex.Message}"); return null; }
      catch (Exception)
      {
        return null;
      }
    }

    private PluginResult ExecuteRequired(string command, object? args, object? context)
    {
      try
      {
        return _runner.ExecuteAsync(_plugin, command, args, context).GetAwaiter().GetResult()
          ?? throw new InvalidOperationException($"Plugin '{_plugin.Name}' returned an invalid response for '{command}'.");
      }
      catch (TimeoutException ex)
      {
        throw new TimeoutException($"Plugin '{_plugin.Name}' timed out while executing '{command}'.", ex);
      }
      catch (TaskCanceledException ex)
      {
        throw new TimeoutException($"Plugin '{_plugin.Name}' timed out while executing '{command}'.", ex);
      }
    }

    private void EnsureSuccess(PluginResult result, string operation, string target)
    {
      if (result.Success)
      {
        return;
      }

      var error = string.IsNullOrWhiteSpace(result.Error) ? "Unknown error." : result.Error;
      throw new Exception($"Plugin '{_plugin.Name}' failed to {operation} '{target}': {error}");
    }

    // Inner class to satisfy the Interface contract
    private class PluginRuntimeBootstrapper : IPackageManagerBootstrapper
    {
      private readonly PluginManifest _p;
      private readonly IPluginManager _m;
      private readonly IRuntimeResolver _r;
      private readonly ILogger? _logger;

      public PluginRuntimeBootstrapper(PluginManifest p, IPluginManager m, IRuntimeResolver r, ILogger? logger)
      {
        _p = p;
        _m = m;
        _r = r;
        _logger = logger;
      }

      public string Name => $"{_p.Name} Runtime";

      public bool IsInstalled()
      {
        // We check if the required runtime is installed
        string exe = "";
        switch (_p.Type.ToLower())
        {
          case "python":
            exe = _r.Resolve("uv");
            break;
          case "typescript":
          case "javascript":
            exe = _r.Resolve("bun");
            break;
          case "executable":
            return true;
          default:
            return true;
        }

        if (string.IsNullOrEmpty(exe)) return false;

        try
        {
          return Process.Start(new ProcessStartInfo { FileName = exe, Arguments = "--version", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true })?.WaitForExit(1000) ?? false;
        }
        catch (global::System.IO.IOException ex)
        {
          _logger?.LogWarning($"Package deployment IO bottleneck: {ex.Message}");
          return false;
        }
        catch (Exception)
        {
          return false;
        }
      }

      public void Install(bool dryRun)
      {
        _m.EnsureRuntimeAsync(_p).GetAwaiter().GetResult();
      }
    }
  }
}
