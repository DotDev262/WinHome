using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using WinHome.Services.Logging;
using WinHome.Services.Managers;
using WinHome.Services.Plugins;
using WinHome.Services.System;
using WinHome.Services;

namespace WinHome.Infrastructure;

public static class AppHost
{
  public static IHost CreateHost(string[] args)
  {
    // Check for --json flag in args to force JSON logging early
    bool isJson = args.Contains("--json");
    LogFileWriter? logFileWriter = null;
    var logFilePath = LogFilePathParser.Parse(args);

    if (logFilePath != null)
    {
      if (!LogFileWriter.TryCreate(logFilePath, out logFileWriter, out var error))
      {
        throw new InvalidOperationException(error);
      }
    }

    return Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
          ConfigureServices(context.Configuration, services, isJson, logFileWriter);
          if (logFileWriter != null)
          {
            services.AddSingleton(logFileWriter);
          }
        })
        .Build();
  }

  public static void ConfigureServices(IConfiguration configuration, IServiceCollection services, bool isJsonForce = false, LogFileWriter? logFileWriter = null)
  {
    var isJsonConfig = configuration.GetValue<bool>("json");
    var isJson = isJsonForce || isJsonConfig;

    if (isJson)
    {
      services.AddSingleton<ILogger>(_ => new JsonLogger(logFileWriter));
    }
    else
    {
      services.AddSingleton<ILogger>(_ => new ConsoleLogger(logFileWriter));
    }

    // System Services
    services.AddSingleton<IProcessRunner, DefaultProcessRunner>();
    services.AddSingleton<IFileSystem, DefaultFileSystem>();
    services.AddSingleton<IServiceControllerWrapper, ServiceControllerWrapper>();
    services.AddSingleton<IRegistryWrapper, RegistryWrapper>();

    // Domain Services
    services.AddSingleton<IConfigValidator, ConfigValidator>();
    services.AddSingleton<IDotfileService, DotfileService>();
    services.AddSingleton<IRegistryService, RegistryService>();
    services.AddSingleton<ISystemSettingsService, SystemSettingsService>();
    services.AddSingleton<IWslService, WslService>();
    services.AddSingleton<IGitService, GitService>();
    services.AddSingleton<IEnvironmentService, EnvironmentService>();
    services.AddSingleton<IWindowsServiceManager, WindowsServiceManager>();
    services.AddSingleton<IScheduledTaskService, ScheduledTaskService>();
    services.AddSingleton<IRuntimeResolver, RuntimeResolver>();
    services.AddSingleton<IUpdateService, UpdateService>();
    services.AddSingleton<ISecretResolver, SecretResolver>();
    services.AddSingleton<IStateService, StateService>();
    // New apply state writer for resumable applies
    services.AddSingleton<StateWriter>();
    services.AddSingleton<IPluginManager>(sp => new PluginManager(
        sp.GetRequiredService<UvBootstrapper>(),
        sp.GetRequiredService<BunBootstrapper>(),
        sp.GetRequiredService<ILogger>(),
        null,
        sp.GetRequiredService<IRuntimeResolver>()
    ));
    services.AddSingleton<IGeneratorService, GeneratorService>();
    services.AddSingleton<IPluginRunner, PluginRunner>();

    // Bootstrappers
    services.AddSingleton<ChocolateyBootstrapper>();
    services.AddSingleton<ScoopBootstrapper>();
    services.AddSingleton<WingetBootstrapper>();
    services.AddSingleton<UvBootstrapper>();
    services.AddSingleton<BunBootstrapper>();

    // Package Managers
    services.AddSingleton<WingetService>(sp => new WingetService(
        sp.GetRequiredService<IProcessRunner>(),
        sp.GetRequiredService<WingetBootstrapper>(),
        sp.GetRequiredService<ILogger>(),
        sp.GetRequiredService<IRuntimeResolver>()
    ));
    services.AddSingleton<ChocolateyService>(sp => new ChocolateyService(
        sp.GetRequiredService<IProcessRunner>(),
        sp.GetRequiredService<ChocolateyBootstrapper>(),
        sp.GetRequiredService<ILogger>(),
        sp.GetRequiredService<IRuntimeResolver>()
    ));
    services.AddSingleton<ScoopService>(sp => new ScoopService(
        sp.GetRequiredService<IProcessRunner>(),
        sp.GetRequiredService<ScoopBootstrapper>(),
        sp.GetRequiredService<ILogger>(),
        sp.GetRequiredService<IRuntimeResolver>()
    ));

    services.AddSingleton<Dictionary<string, IPackageManager>>(sp => new()
        {
            { "winget", sp.GetRequiredService<WingetService>() },
            { "choco", sp.GetRequiredService<ChocolateyService>() },
            { "scoop", sp.GetRequiredService<ScoopService>() }
        });

    services.AddSingleton<Engine>(sp => new Engine(
        sp.GetRequiredService<Dictionary<string, IPackageManager>>(),
        sp.GetRequiredService<IDotfileService>(),
        sp.GetRequiredService<IRegistryService>(),
        sp.GetRequiredService<ISystemSettingsService>(),
        sp.GetRequiredService<IWslService>(),
        sp.GetRequiredService<IGitService>(),
        sp.GetRequiredService<IEnvironmentService>(),
        sp.GetRequiredService<IWindowsServiceManager>(),
        sp.GetRequiredService<IScheduledTaskService>(),
        sp.GetRequiredService<IPluginManager>(),
        sp.GetRequiredService<IPluginRunner>(),
        sp.GetRequiredService<IStateService>(),
        sp.GetRequiredService<ILogger>(),
        sp.GetRequiredService<IRuntimeResolver>(),
        sp.GetRequiredService<StateWriter>()
    ));
    services.AddSingleton<AppRunner>(sp => new AppRunner(
        sp.GetRequiredService<Engine>(),
        sp.GetRequiredService<IConfigValidator>(),
        sp.GetRequiredService<ISecretResolver>(),
        sp.GetRequiredService<ILogger>()
    ));
  }
}
