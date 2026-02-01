using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinHome.Interfaces;
using WinHome.Services.Bootstrappers;
using WinHome.Services.Logging;
using WinHome.Services.Managers;
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

        // Bootstrappers
        services.AddSingleton<ChocolateyBootstrapper>();
        services.AddSingleton<ScoopBootstrapper>();
        services.AddSingleton<WingetBootstrapper>();

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

        services.AddSingleton<Engine>();
        services.AddSingleton<AppRunner>();
    }
}
