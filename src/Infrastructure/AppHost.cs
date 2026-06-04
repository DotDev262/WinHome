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

/// <summary>Configures the dependency injection container and builds the application host.</summary>
public static class AppHost
{
  /// <summary>Creates and configures the application <see cref="IHost"/> with all services registered.</summary>
  /// <param name="args">Command-line arguments used to detect early flags (e.g. --json).</param>
  /// <returns>A configured <see cref="IHost"/> ready for service resolution.</returns>
  public static IHost CreateHost(string[] args)
  {
    bool isJson = args.Contains("--json");

    return Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
          ConfigureServices(context.Configuration, services, isJson);
        })
        .Build();
  }

  /// <summary>Registers all application services into the DI container.</summary>
  /// <param name="configuration">Application configuration source.</param>
  /// <param name="services">The service collection to register into.</param>
  /// <param name="isJsonForce">If <c>true</c>, forces JSON logging regardless of configuration.</param>
  public static void ConfigureServices(IConfiguration configuration, IServiceCollection services, bool isJsonForce = false)
  {
    var isJsonConfig = configuration.GetValue<bool>("json");
    var isJson = isJsonForce || isJsonConfig;

    if (isJson)
    {
      services.AddSingleton<ILogger, JsonLogger>();
    }
    else
    {
      services.AddSingleton<ILogger, ConsoleLogger>();
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
