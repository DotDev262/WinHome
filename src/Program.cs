using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Models;
using WinHome.Services.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>Main entry point for the WinHome CLI application.</summary>
class Program
{
  /// <summary>Parses CLI arguments and dispatches to the appropriate command handler.</summary>
  /// <param name="args">Command-line arguments.</param>
  /// <returns>Exit code (0 for success, non-zero for errors).</returns>
  static async Task<int> Main(string[] args)
  {
    try
    {
      // Post-update recovery: handle interrupted self-updates
      PerformPostUpdateRecovery();

      if (Array.IndexOf(args, "--version") >= 0)
      {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        string versionString = version is not null
            ? $"WinHome v{version.Major}.{version.Minor}.{version.Build}"
            : "WinHome v1.0.0";
        Console.WriteLine(versionString);
        return 0;
      }

      using IHost host = AppHost.CreateHost(args);

      var rootCommand = CliBuilder.BuildRootCommand(
          // Run Action
          async (file, dryRun, profile, debug, diff, json, update, force, continueOnError, minLogLevel) =>
          {
            var logger = host.Services.GetRequiredService<ILogger>();
            logger.SetMinLevel(minLogLevel);

            if (update)
            {
              var updater = host.Services.GetRequiredService<IUpdateService>();
              var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.2.0";
              if (await updater.CheckForUpdatesAsync(currentVersion))
              {
                await updater.UpdateAsync();
              }
              return 0;
            }

            var runner = host.Services.GetRequiredService<AppRunner>();

            var exitCode = await runner.RunAsync(file, dryRun, profile, debug, diff, json, force, continueOnError);

            if (logger is JsonLogger jsonLogger)
            {
              Console.WriteLine(jsonLogger.ToJson());
            }

            return exitCode;
          },
          // Generate Action
          async (outputFile, minLogLevel) =>
          {
            var logger = host.Services.GetRequiredService<ILogger>();
            logger.SetMinLevel(minLogLevel);
            var generator = host.Services.GetRequiredService<IGeneratorService>();

            try
            {
              var config = await generator.GenerateAsync();

              var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                        .Build();

              var yaml = serializer.Serialize(config);

              if (outputFile != null)
              {
                string? dir = Path.GetDirectoryName(outputFile.FullName);
                if (dir != null) Directory.CreateDirectory(dir);
                string tmp = outputFile.FullName + ".tmp";
                await File.WriteAllTextAsync(tmp, yaml);
                File.Move(tmp, outputFile.FullName, overwrite: true);
                logger.LogSuccess($"[Generator] Configuration saved to {outputFile.FullName}");
              }
              else
              {
                Console.WriteLine(yaml);
              }
              return 0;
            }
            catch (Exception ex)
            {
              logger.LogError($"[Generator] Failed to generate configuration: {ex.Message}");
              return 1;
            }
          },
          // State Action
          async (command, path, minLogLevel) =>
          {
            var logger = host.Services.GetRequiredService<ILogger>();
            logger.SetMinLevel(minLogLevel);
            var stateService = host.Services.GetRequiredService<IStateService>();

            switch (command)
            {
              case "list":
                var items = stateService.ListItems();
                if (!items.Any())
                {
                  logger.LogInfo("[State] No items are currently managed by WinHome.");
                }
                else
                {
                  logger.LogInfo("\n--- Managed Items ---");
                  foreach (var item in items)
                  {
                    Console.WriteLine($"  - {item}");
                  }
                }
                break;
              case "backup":
                if (string.IsNullOrEmpty(path)) return 1;
                stateService.BackupState(path);
                break;
              case "restore":
                if (string.IsNullOrEmpty(path)) return 1;
                stateService.RestoreState(path);
                break;
              case "clear":
                try
                {
                  stateService.SaveState(new StateData());
                  stateService.LoadState();
                  logger.LogSuccess("[State] All tracking state cleared successfully.");
                }
                catch (Exception ex)
                {
                  logger.LogError($"[State] Failed to clear tracking state: {ex.Message}");
                  return 1;
                }
                break;
            }
            return 0;
          },

          // Config Backup Action
          async (provider, path, _, minLogLevel) =>
          {
            var logger = host.Services.GetRequiredService<ILogger>();
            logger.SetMinLevel(minLogLevel);

            var backupService = host.Services.GetRequiredService<IConfigBackupService>();

            try
            {
              if (provider == "restore")
              {
                var (restoredProvider, restoredSettings) =
                    await backupService.RestoreAsync(path!);

                var existingConfigFile = new FileInfo("config.yaml");

                if (!existingConfigFile.Exists)
                {
                  logger.LogError("[Config] config.yaml not found.");
                  return 1;
                }

                var existingYaml =
                    await File.ReadAllTextAsync(existingConfigFile.FullName);

                var restoreDeserializer = new DeserializerBuilder()
                  .WithNamingConvention(CamelCaseNamingConvention.Instance)
                  .Build();

                var existingConfig =
                    restoreDeserializer.Deserialize<Configuration>(existingYaml);

                existingConfig.Extensions[restoredProvider] =
                    restoredSettings!;

                var serializer = new SerializerBuilder()
                  .WithNamingConvention(CamelCaseNamingConvention.Instance)
                  .Build();

                var yaml = serializer.Serialize(existingConfig);

                var tmp = "config.yaml.tmp";

                await File.WriteAllTextAsync(tmp, yaml);

                File.Move(
                  tmp,
                  "config.yaml",
                  true);

                logger.LogSuccess(
                    $"[Config] Provider '{restoredProvider}' restored.");

                return 0;
              }

              var configFile = new FileInfo("config.yaml");

              if (!configFile.Exists)
              {
                logger.LogError("[Config] config.yaml not found. Run generate first.");
                return 1;
              }

              var yamlContent = await File.ReadAllTextAsync(configFile.FullName);

              var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

              var config = deserializer.Deserialize<Configuration>(yamlContent);

              if (config == null)
              {
                logger.LogError("[Config] Invalid configuration file.");
                return 1;
              }

              await backupService.BackupAsync(
              provider,
              config,
              path!);

              logger.LogSuccess($"[Config] Backup created: {path}");

              return 0;
            }
            catch (Exception ex)
            {
              logger.LogError($"[Config] Failed: {ex.Message}");
              return 1;
            }
          }
      );

      // Register status command (reads .winhome-state.json)
      StatusCommand.Register(rootCommand, host.Services);

      return await rootCommand.Parse(args).InvokeAsync();
    }
    catch (Exception ex)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"[Fatal Error] An unhandled exception occurred:");
      Console.WriteLine(ex.Message);

      if (args.Contains("--debug"))
      {
        Console.WriteLine(ex.StackTrace);
      }
      Console.ResetColor();
      return 1;
    }
  }

  /// <summary>Detects and recovers from an interrupted self-update on startup.</summary>
  private static void PerformPostUpdateRecovery()
  {
    try
    {
      string currentPath = Environment.ProcessPath ?? string.Empty;
      if (string.IsNullOrEmpty(currentPath)) return;

      string? currentDir = Path.GetDirectoryName(currentPath);
      if (string.IsNullOrEmpty(currentDir)) return;

      string exeName = Path.GetFileName(currentPath);
      string oldPath = Path.Combine(currentDir, exeName + ".old");
      string stagingPath = Path.Combine(currentDir, exeName + ".staging");

      bool currentExists = File.Exists(currentPath);
      bool oldExists = File.Exists(oldPath);
      bool stagingExists = File.Exists(stagingPath);

      if (stagingExists)
      {
        TryDeleteFile(stagingPath);
      }

      if (!oldExists) return;

      if (!currentExists)
      {
        try
        {
          File.Move(oldPath, currentPath);
          Console.Error.WriteLine("[Recovery] Restored previous version from interrupted update.");
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine($"[Recovery] Failed to restore from .old backup: {ex.Message}");
        }
      }
      else
      {
        TryDeleteFile(oldPath);
      }
    }
    catch
    {
    }
  }

  private static void TryDeleteFile(string path)
  {
    try
    {
      if (File.Exists(path)) File.Delete(path);
    }
    catch
    {
    }
  }
}
