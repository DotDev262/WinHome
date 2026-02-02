using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using WinHome.Services.Logging;
using WinHome.Services.Managers;
using WinHome.Services.Plugins;
using WinHome.Services.System;

namespace WinHome.Infrastructure;

public static class AppHost
{
    public static IHost CreateHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .Build();
    }

    public static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        var isJson = configuration.GetValue<bool>("json");

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
        services.AddSingleton<IPluginManager>(sp => new PluginManager(
            sp.GetRequiredService<UvBootstrapper>(),
            sp.GetRequiredService<BunBootstrapper>(),
            sp.GetRequiredService<ILogger>(),
            null
        ));
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
            sp.GetRequiredService<ILogger>()
        ));
        services.AddSingleton<ChocolateyService>(sp => new ChocolateyService(
            sp.GetRequiredService<IProcessRunner>(),
            sp.GetRequiredService<ChocolateyBootstrapper>(),
            sp.GetRequiredService<ILogger>()
        ));
        services.AddSingleton<ScoopService>(sp => new ScoopService(
            sp.GetRequiredService<IProcessRunner>(),
            sp.GetRequiredService<ScoopBootstrapper>(),
            sp.GetRequiredService<ILogger>()
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
            sp.GetRequiredService<ILogger>()
        ));
        services.AddSingleton<AppRunner>();
    }
}
