using System.Collections.Concurrent;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services;

namespace WinHome
{
  /// <summary>Main orchestrator for WinHome configuration application. Coordinates all services: apps, registry, WSL, Git, env vars, services, scheduled tasks, dotfiles, plugins, and system settings.</summary>
  public class Engine : IEngine
  {
    private readonly Dictionary<string, IPackageManager> _managers;
    private readonly IDotfileService _dotfiles;
    private readonly IRegistryService _registry;
    private readonly ISystemSettingsService _systemSettings;
    private readonly IWslService _wsl;
    private readonly IGitService _git;
    private readonly IEnvironmentService _env;
    private readonly IWindowsServiceManager _serviceManager;
    private readonly IScheduledTaskService _scheduledTaskService;
    private readonly ILogger _logger;
    private readonly IPluginManager _pluginManager;
    private readonly IPluginRunner _pluginRunner;
    private readonly IStateService _stateService;
    private readonly IRuntimeResolver _runtimeResolver;
    private readonly StateWriter _stateWriter;

    /// <summary>Initializes a new instance of <see cref="Engine"/> with all required service dependencies.</summary>
    public Engine(
        Dictionary<string, IPackageManager> managers,
        IDotfileService dotfiles,
        IRegistryService registry,
        ISystemSettingsService systemSettings,
        IWslService wsl,
        IGitService git,
        IEnvironmentService env,
        IWindowsServiceManager serviceManager,
        IScheduledTaskService scheduledTaskService,
        IPluginManager pluginManager,
        IPluginRunner pluginRunner,
        IStateService stateService,
        ILogger logger,
        IRuntimeResolver runtimeResolver)
    {
      _managers = managers;
      _dotfiles = dotfiles;
      _registry = registry;
      _systemSettings = systemSettings;
      _wsl = wsl;
      _git = git;
      _env = env;
      _serviceManager = serviceManager;
      _scheduledTaskService = scheduledTaskService;
      _pluginManager = pluginManager;
      _pluginRunner = pluginRunner;
      _stateService = stateService;
      _logger = logger;
      _runtimeResolver = runtimeResolver;
      _stateWriter = new StateWriter();
    }

    /// <summary>Initializes a new instance with an optional <see cref="StateWriter"/> for resumable applies.</summary>
    public Engine(
        Dictionary<string, IPackageManager> managers,
        IDotfileService dotfiles,
        IRegistryService registry,
        ISystemSettingsService systemSettings,
        IWslService wsl,
        IGitService git,
        IEnvironmentService env,
        IWindowsServiceManager serviceManager,
        IScheduledTaskService scheduledTaskService,
        IPluginManager pluginManager,
        IPluginRunner pluginRunner,
        IStateService stateService,
        ILogger logger,
        IRuntimeResolver runtimeResolver,
        StateWriter? stateWriter)
        : this(managers, dotfiles, registry, systemSettings, wsl, git, env, serviceManager, scheduledTaskService, pluginManager, pluginRunner, stateService, logger, runtimeResolver)
    {
      _stateWriter = stateWriter ?? new StateWriter();
    }

    /// <summary>Applies the given configuration: installs apps, applies registry tweaks, configures WSL/Git/env/services/tasks, links dotfiles, runs plugins, and applies system settings.</summary>
    /// <param name="config">The configuration to apply.</param>
    /// <param name="dryRun">If <c>true</c>, previews changes without making any.</param>
    /// <param name="profileName">Optional named profile to activate.</param>
    /// <param name="debug">If <c>true</c>, shows detailed error information.</param>
    /// <param name="diff">If <c>true</c>, shows a diff of changes and returns without applying.</param>
    /// <param name="forceReapply">If <c>true</c>, reapplies steps even if previously succeeded.</param>
    /// <param name="continueOnError">If <c>true</c>, continues with remaining steps when a step fails.</param>
    public async Task RunAsync(Configuration config, bool dryRun, string? profileName = null, bool debug = false, bool diff = false, bool forceReapply = false, bool continueOnError = false)
    {
      _logger.LogInfo($"--- WinHome v{config.Version} ---");

      // Load Plugins
      var plugins = _pluginManager.DiscoverPlugins().ToList();
      var loggedPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var plugin in plugins)
      {
        if (plugin.Capabilities.Contains("package_manager"))
        {
          if (!_managers.ContainsKey(plugin.Name))
          {
            _managers[plugin.Name] = new WinHome.Services.Plugins.PluginPackageManagerAdapter(plugin, _pluginRunner, _pluginManager, _runtimeResolver, _logger);
          }
        }
      }

      if (!string.IsNullOrEmpty(profileName))
      {
        if (config.Profiles != null && config.Profiles.TryGetValue(profileName, out var profile))
        {
          _logger.LogInfo($"\n[Profile] Activating '{profileName}'...");
          if (profile.Git != null) config.Git = profile.Git;
          ApplyProfileEnvironmentOverrides(config, profile);
        }
        else
        {
          _logger.LogError($"[Error] Profile '{profileName}' not found.");
          if (!dryRun) return;
        }
      }

      if (diff)
      {
        await PrintDiffAsync(config);
        return;
      }

      // Check network if we have apps to install or WSL update enabled
      if ((config.Apps.Any() || (config.Wsl != null && config.Wsl.Update)) && !dryRun)
      {
        if (!await WaitForNetwork())
        {
          _logger.LogWarning("[Warning] No internet connection detected. Package manager operations may fail.");
        }
      }

      var currentState = await BuildStateFromConfig(config);

      var previousState = _stateService.LoadState();
      currentState.SystemSettingOriginals = new Dictionary<string, object>(previousState.SystemSettingOriginals);
      var confirmedApplied = new ConcurrentDictionary<string, byte>(
          previousState.AppliedItems
              .Where(x => x != null)
              .Select(x => new KeyValuePair<string, byte>(x, (byte)0)),
          StringComparer.OrdinalIgnoreCase);
      var hadSuccessfulApply = false;

      // Cleanup
      var itemsToRemove = previousState.AppliedItems.Except(currentState.AppliedItems).ToList();
      if (itemsToRemove.Any())
      {
        _logger.LogInfo("\n--- Cleaning Up ---");
        var removedItems = new ConcurrentBag<string>();
        await Task.Run(() => Parallel.ForEach(itemsToRemove, uniqueId =>
        {
          if (uniqueId.StartsWith("reg:"))
          {
            var parts = uniqueId.Substring(4).Split('|', 2);
            if (parts.Length == 2 && _registry.Revert(parts[0], parts[1], dryRun) && !dryRun)
            {
              removedItems.Add(uniqueId);
              confirmedApplied.TryRemove(uniqueId, out _);
            }
          }
          else
          {
            var parts = uniqueId.Split(':', 2);
            if (parts.Length == 2 && _managers.TryGetValue(parts[0], out var mgr))
            {
              mgr.Uninstall(parts[1], dryRun);
              if (!dryRun)
              {
                removedItems.Add(uniqueId);
                confirmedApplied.TryRemove(uniqueId, out _);
              }
            }
          }
        }));

        foreach (var item in removedItems)
        {
          _stateService.RemoveApplied(item);
          try
          {
            _stateWriter.RemoveStep(item);
          }
          catch (Exception ex)
          {
            _logger.LogWarning($"[Engine] Failed to remove step: {ex.Message}");
          }
        }
      }

      // Revert system settings that are no longer in config
      if (OperatingSystem.IsWindows() && previousState.SystemSettingOriginals.Any())
      {
        var removedSystemSettings = previousState.SystemSettingOriginals.Keys
            .Where(k => !config.SystemSettings.ContainsKey(k))
            .ToList();

        if (removedSystemSettings.Any())
        {
          _logger.LogInfo("\n--- Reverting Removed System Settings ---");
          foreach (var settingKey in removedSystemSettings)
          {
            var originalValue = previousState.SystemSettingOriginals[settingKey];
            await _systemSettings.RevertSystemSettingAsync(settingKey, originalValue, dryRun);
            if (!dryRun)
            {
              _stateService.RemoveSystemSettingOriginal(settingKey);
              currentState.SystemSettingOriginals.Remove(settingKey);
            }
          }
        }
      }

      // 1. Ensure System Managers (Scoop) are ready if needed by plugins
      if (plugins.Any(p => p.Type.ToLower() == "python" || p.Type.ToLower() == "javascript" || p.Type.ToLower() == "typescript"))
      {
        if (_managers.TryGetValue("scoop", out var scoopMgr))
        {
          if (!scoopMgr.IsAvailable())
          {
            _logger.LogInfo("\n--- Bootstrapping System Managers ---");
            _logger.LogInfo("[Engine] Bootstrapping Scoop for plugin runtimes...");
            scoopMgr.Bootstrapper.Install(dryRun);
          }
        }
      }

      // 2. Reconcile Plugin Runtimes
      var pluginsNeedingRuntime = plugins.Where(p => !p.Type.Equals("executable", StringComparison.OrdinalIgnoreCase)).ToList();
      if (pluginsNeedingRuntime.Any())
      {
        var usedPluginNames = config.Apps.Select(a => a.Manager)
            .Concat(new[] { "vim", "vscode", "obsidian", "ohmyposh" }.Where(_ => config.Vim != null || config.Vscode != null || config.Obsidian != null || config.Ohmyposh != null))
            .Concat(config.Extensions.Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var usedPluginsNeedingRuntime = pluginsNeedingRuntime.Where(p => usedPluginNames.Contains(p.Name)).ToList();

        if (usedPluginsNeedingRuntime.Any())
        {
          _logger.LogInfo("\n--- Reconciling Plugin Runtimes ---");
          foreach (var plugin in usedPluginsNeedingRuntime)
          {
            await _pluginManager.EnsureRuntimeAsync(plugin);
          }
          _env.RefreshPath();
        }
      }

      // Build a global set of all resourceIds across every collection.
      // Passed to DependencyResolver.Sort so cross-type dependsOn references
      // (e.g. a service depending on an app) are validated without error.
      var globalResourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var r in config.Apps)           if (r.ResourceId is not null) globalResourceIds.Add(r.ResourceId);
      foreach (var r in config.EnvVars)        if (r.ResourceId is not null) globalResourceIds.Add(r.ResourceId);
      foreach (var r in config.Dotfiles)       if (r.ResourceId is not null) globalResourceIds.Add(r.ResourceId);
      foreach (var r in config.RegistryTweaks) if (r.ResourceId is not null) globalResourceIds.Add(r.ResourceId);
      foreach (var r in config.Services)       if (r.ResourceId is not null) globalResourceIds.Add(r.ResourceId);
      foreach (var r in config.ScheduledTasks) if (r.ResourceId is not null) globalResourceIds.Add(r.ResourceId);

      // Install Apps
      if (config.Apps.Any())
      {
        _logger.LogInfo("\n--- Reconciling Apps ---");
        var applyState = _stateWriter.Load();

        var sortedApps = DependencyResolver.Sort(config.Apps, globalResourceIds);
        foreach (var app in sortedApps)
        {
          var stepId = $"{app.Manager}:{app.Id}";
          _logger.LogInfo($"[Engine] Processing {stepId}...");

          if (!forceReapply && !dryRun && applyState.TryGetValue(stepId, out var previous) && previous.Status == StepStatus.Succeeded)
          {
            _logger.LogInfo($"[Engine] Skipping previously applied {stepId}.");
            var skippedResult = new StepResult
            {
              StepId = stepId,
              StepType = "app",
              StepName = app.Id,
              Status = StepStatus.Skipped,
              AppliedAt = previous.AppliedAt
            };
            applyState[stepId] = skippedResult;
            continue;
          }

          if (_managers.TryGetValue(app.Manager, out var mgr))
          {
            try
            {
              if (mgr is WinHome.Services.Plugins.PluginPackageManagerAdapter adapter)
              {
                if (loggedPlugins.Add(app.Manager))
                {
                  _logger.LogInfo($"[Plugin] Discovered: {app.Manager} ({adapter.PluginType})");
                }
              }

              if (!mgr.IsAvailable())
              {
                _logger.LogInfo($"[Engine] Manager '{app.Manager}' not available. Bootstrapping...");
                mgr.Bootstrapper.Install(dryRun);
                if (!mgr.IsAvailable())
                {
                  _logger.LogError($"[Error] Manager '{app.Manager}' not found after attempting to install it.");
                  continue;
                }
              }

              mgr.Install(app, dryRun);

              if (!dryRun)
              {
                var successResult = new StepResult
                {
                  StepId = stepId,
                  StepType = "app",
                  StepName = app.Id,
                  Status = StepStatus.Succeeded,
                  AppliedAt = DateTime.UtcNow
                };
                _stateWriter.RecordStep(successResult);
                applyState[stepId] = successResult;

                confirmedApplied.TryAdd(stepId, (byte)0);
                hadSuccessfulApply = true;
              }

              _env.RefreshPath();
            }
            catch (Exception ex)
            {
              var original = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
              var failedResult = new StepResult
              {
                StepId = stepId,
                StepType = "app",
                StepName = app.Id,
                Status = StepStatus.Failed,
                ErrorMessage = ex.Message,
                AppliedAt = DateTime.UtcNow
              };

              try
              {
                _stateWriter.RecordStep(failedResult);
              }
              catch (Exception stateEx)
              {
                _logger.LogWarning($"[Engine] Failed to write failed step status for {stepId}: {stateEx.Message}");
              }
              applyState[stepId] = failedResult;

              _logger.LogError($"[Error] Failed applying {stepId}: {ex.Message}");
              if (!continueOnError) original.Throw();
            }
          }
          else
          {
            _logger.LogError($"[Error] Unknown manager: {app.Manager}");
          }
        }
      }

      if (config.Git != null) _git.Configure(config.Git, dryRun);

      if (config.Wsl != null)
      {
        _logger.LogInfo("\n--- Configuring WSL ---");
        _wsl.Configure(config.Wsl, dryRun);
      }

      if (config.EnvVars.Any())
      {
        _logger.LogInfo("\n--- Configuring Environment Variables ---");
        var sortedEnvVars = DependencyResolver.Sort(config.EnvVars, globalResourceIds);
        foreach (var env in sortedEnvVars)
        {
          _env.Apply(env, dryRun);
        }
      }

      // Plugin Extensions
      var allExtensions = new Dictionary<string, object>(config.Extensions);
      if (config.Vim != null) allExtensions["vim"] = config.Vim;
      if (config.Vscode != null) allExtensions["vscode"] = config.Vscode;
      if (config.Obsidian != null) allExtensions["obsidian"] = config.Obsidian;
      if (config.Ohmyposh != null) allExtensions["ohmyposh"] = config.Ohmyposh;

      if (allExtensions.Any())
      {
        _logger.LogInfo("\n--- Running Plugin Extensions ---");
        foreach (var ext in allExtensions)
        {
          var pluginName = ext.Key;
          var pluginConfig = ext.Value;

          var plugin = plugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));

          if (plugin != null)
          {
            if (loggedPlugins.Add(plugin.Name))
            {
              _logger.LogInfo($"[Plugin] Discovered: {plugin.Name} ({plugin.Type})");
            }

            await _pluginManager.EnsureRuntimeAsync(plugin);
            _logger.LogInfo($"[Plugin] Applying configuration for '{pluginName}'...");
            var result = await _pluginRunner.ExecuteAsync(plugin, "apply", pluginConfig, new { dryRun = dryRun });

            if (!result.Success)
            {
              _logger.LogError($"[Error] Plugin '{pluginName}' failed: {result.Error}");
            }
            else if (result.Changed)
            {
              _logger.LogSuccess($"[Plugin] '{pluginName}' applied successfully.");
            }
          }
          else
          {
            _logger.LogWarning($"[Warning] Configuration found for '{pluginName}' but no matching plugin is installed.");
          }
        }
      }

      var presetTweaks = await _systemSettings.GetTweaksAsync(config.SystemSettings);
      var allTweaks = config.RegistryTweaks.Concat(presetTweaks).ToList();

      if (allTweaks.Any() && OperatingSystem.IsWindows())
      {
        _logger.LogInfo("\n--- Applying Registry Tweaks ---");
        var applyState = _stateWriter.Load();
        foreach (var tweak in allTweaks)
        {
          var stepId = $"reg:{tweak.Path}|{tweak.Name}";

          if (!forceReapply && !dryRun && applyState.TryGetValue(stepId, out var previous) && previous.Status == StepStatus.Succeeded)
          {
            _logger.LogInfo($"[Engine] Skipping previously applied registry tweak {tweak.Path}|{tweak.Name}.");
            var skippedResult = new StepResult
            {
              StepId = stepId,
              StepType = "registry",
              StepName = tweak.Name,
              Status = StepStatus.Skipped,
              AppliedAt = previous.AppliedAt
            };
            applyState[stepId] = skippedResult;
            continue;
          }

          try
          {
            var applied = _registry.Apply(tweak, dryRun);
            if (!applied)
            {
              throw new Exception($"Failed to apply registry tweak {tweak.Path}|{tweak.Name}.");
            }

            if (!dryRun)
            {
              var successResult = new StepResult
              {
                StepId = stepId,
                StepType = "registry",
                StepName = tweak.Name,
                Status = StepStatus.Succeeded,
                AppliedAt = DateTime.UtcNow
              };
              _stateWriter.RecordStep(successResult);
              applyState[stepId] = successResult;

              confirmedApplied.TryAdd(stepId, (byte)0);
              hadSuccessfulApply = true;
            }
          }
          catch (Exception ex)
          {
            var original = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
            var failedResult = new StepResult
            {
              StepId = stepId,
              StepType = "registry",
              StepName = tweak.Name,
              Status = StepStatus.Failed,
              ErrorMessage = ex.Message,
              AppliedAt = DateTime.UtcNow
            };

            try
            {
              _stateWriter.RecordStep(failedResult);
            }
            catch (Exception stateEx)
            {
              _logger.LogWarning($"[Engine] Failed to write failed step status for {stepId}: {stateEx.Message}");
            }
            applyState[stepId] = failedResult;

            _logger.LogError($"[Error] Registry tweak failed: {ex.Message}");
            if (!continueOnError) original.Throw();
          }
        }
      }

      if (config.SystemSettings.Any() && OperatingSystem.IsWindows())
      {
        _logger.LogInfo("\n--- Applying System Settings ---");

        if (!dryRun)
        {
          var originals = await _systemSettings.CaptureOriginalSettingsAsync(config.SystemSettings);
          foreach (var kvp in originals)
          {
            if (!currentState.SystemSettingOriginals.ContainsKey(kvp.Key))
            {
              currentState.SystemSettingOriginals[kvp.Key] = kvp.Value;
              _stateService.TrackSystemSettingOriginal(kvp.Key, kvp.Value);
            }
          }
        }

        await _systemSettings.ApplyNonRegistrySettingsAsync(config.SystemSettings, dryRun);
      }

      if (config.Dotfiles.Any())
      {
        _logger.LogInfo("\n--- Linking Dotfiles ---");
        var sortedDotfiles = DependencyResolver.Sort(config.Dotfiles, globalResourceIds);
        var hasDotfileDeps = sortedDotfiles.Any(d => d.DependsOn?.Count > 0);
        if (hasDotfileDeps)
        {
          foreach (var dotfile in sortedDotfiles)
            _dotfiles.Apply(dotfile, dryRun);
        }
        else
        {
          await Task.Run(() => Parallel.ForEach(sortedDotfiles, dotfile => _dotfiles.Apply(dotfile, dryRun)));
        }
      }

      if (config.Services.Any())
      {
        _logger.LogInfo("\n--- Managing Windows Services ---");
        var sortedServices = DependencyResolver.Sort(config.Services, globalResourceIds);
        var hasServiceDeps = sortedServices.Any(s => s.DependsOn?.Count > 0);
        if (hasServiceDeps)
        {
          foreach (var service in sortedServices)
            _serviceManager.Apply(service, dryRun);
        }
        else
        {
          await Task.Run(() => Parallel.ForEach(sortedServices, service => _serviceManager.Apply(service, dryRun)));
        }
      }

      if (config.ScheduledTasks.Any())
      {
        _logger.LogInfo("\n--- Scheduling Tasks ---");
        var sortedTasks = DependencyResolver.Sort(config.ScheduledTasks, globalResourceIds);
        var hasTaskDeps = sortedTasks.Any(t => t.DependsOn?.Count > 0);
        if (hasTaskDeps)
        {
          foreach (var task in sortedTasks)
            _scheduledTaskService.Apply(task, dryRun);
        }
        else
        {
          await Task.Run(() => Parallel.ForEach(sortedTasks, task => _scheduledTaskService.Apply(task, dryRun)));
        }
      }

      if (!dryRun)
      {
        if (hadSuccessfulApply)
        {
          currentState.AppliedItems = new HashSet<string>(confirmedApplied.Keys, StringComparer.OrdinalIgnoreCase);
          _stateService.SaveState(currentState);
        }

        _logger.LogSuccess("\n[State Synced] Applied changes flushed.");
      }
      else
      {
        _logger.LogWarning("\n[Dry Run] State was NOT saved.");
      }
    }

    /// <summary>Prints a diff of what will change compared to the previously applied state.</summary>
    public async Task PrintDiffAsync(Configuration config)
    {
      _logger.LogInfo("\n--- State Diff ---");

      var previousState = _stateService.LoadState();
      var currentState = await BuildStateFromConfig(config);

      var itemsToRemove = previousState.AppliedItems.Except(currentState.AppliedItems).ToList();
      var itemsToAdd = currentState.AppliedItems.Except(previousState.AppliedItems).ToList();
      var unchangedItems = previousState.AppliedItems.Intersect(currentState.AppliedItems).ToList();

      var systemSettingsReverts = previousState.SystemSettingOriginals.Keys
          .Where(k => !config.SystemSettings.ContainsKey(k))
          .ToList();

      if (!itemsToRemove.Any() && !itemsToAdd.Any() && !systemSettingsReverts.Any())
      {
        _logger.LogSuccess("No changes detected. System is up to date.");
        return;
      }

      if (itemsToRemove.Any())
      {
        _logger.LogError("\n[-] Items to Remove:");
        foreach (var item in itemsToRemove)
          _logger.LogError($"  - {FormatFriendlyName(item)}");
      }

      if (systemSettingsReverts.Any())
      {
        _logger.LogError("\n[-] System Settings to Revert:");
        foreach (var setting in systemSettingsReverts)
        {
          var originalValue = previousState.SystemSettingOriginals[setting];
          _logger.LogError($"  - {setting} → {originalValue}");
        }
      }

      if (itemsToAdd.Any())
      {
        _logger.LogSuccess("\n[+] Items to Add:");
        foreach (var item in itemsToAdd)
          _logger.LogSuccess($"  + {FormatFriendlyName(item)}");
      }

      if (unchangedItems.Any())
      {
        _logger.LogInfo("\n[=] Unchanged Items:");
        foreach (var item in unchangedItems)
          _logger.LogInfo($"  = {FormatFriendlyName(item)}");
      }
    }

    private string FormatFriendlyName(string item)
    {
      if (item.StartsWith("reg:"))
      {
        var parts = item.Substring(4).Split('|', 2);
        if (parts.Length == 2)
        {
          string path = parts[0];
          string name = parts[1];
          string? settingKey = _systemSettings.GetFriendlyName(path, name);
          if (settingKey != null) return $"System Setting: {settingKey}";
          return $"Registry Tweak: {path} -> {name}";
        }
      }
      else
      {
        var parts = item.Split(':', 2);
        if (parts.Length == 2) return $"App ({parts[0]}): {parts[1]}";
      }
      return item;
    }

    private static void ApplyProfileEnvironmentOverrides(Configuration config, ProfileConfig profile)
    {
      if (!profile.EnvVars.Any()) return;

      foreach (var profileEnv in profile.EnvVars.Where(env => !string.IsNullOrWhiteSpace(env.Variable)))
      {
        if (string.Equals(profileEnv.Action, "set", StringComparison.OrdinalIgnoreCase))
        {
          config.EnvVars = config.EnvVars
              .Where(env => !string.Equals(env.Variable, profileEnv.Variable, StringComparison.OrdinalIgnoreCase))
              .ToList();
          config.EnvVars.Add(profileEnv);
          continue;
        }

        var alreadyConfigured = config.EnvVars.Any(env =>
            string.Equals(env.Variable, profileEnv.Variable, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(env.Action, profileEnv.Action, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(env.Value, profileEnv.Value, StringComparison.OrdinalIgnoreCase));

        if (!alreadyConfigured) config.EnvVars.Add(profileEnv);
      }
    }

    private async Task<StateData> BuildStateFromConfig(Configuration config)
    {
      var state = new StateData();

      foreach (var app in config.Apps)
        state.AppliedItems.Add($"{app.Manager}:{app.Id}");

      var presetTweaks = await _systemSettings.GetTweaksAsync(config.SystemSettings);
      var allTweaks = config.RegistryTweaks.Concat(presetTweaks).ToList();
      foreach (var reg in allTweaks)
        state.AppliedItems.Add($"reg:{reg.Path}|{reg.Name}");

      return state;
    }

    private async Task<bool> WaitForNetwork(int timeoutSeconds = 30, CancellationToken cancellationToken = default)
    {
      _logger.LogInfo("[Engine] Checking for internet connectivity...");
      var start = DateTime.Now;
      while ((DateTime.Now - start).TotalSeconds < timeoutSeconds)
      {
        try
        {
          using var ping = new System.Net.NetworkInformation.Ping();
          var reply = ping.Send("1.1.1.1", 2000);
          if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
          {
            _logger.LogSuccess("[Engine] Internet connection verified.");
            return true;
          }
        }
        catch (Exception) { /* Ping failed - will retry */ }

        _logger.LogInfo("[Engine] Waiting for network...");
        await Task.Delay(2000, cancellationToken);
      }
      return false;
    }
  }
}