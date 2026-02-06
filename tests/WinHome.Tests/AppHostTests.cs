using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinHome.Infrastructure;
using WinHome.Interfaces;
using WinHome.Services.Logging;
using Xunit;

namespace WinHome.Tests;

public class AppHostTests
{
    [Fact]
    public void ConfigureServices_ShouldRegisterConsoleLogger_ByDefaults()
    {
        // Arrange
        var args = new string[] { };
        
        // Act
        using var host = AppHost.CreateHost(args);
        var logger = host.Services.GetRequiredService<ILogger>();

        // Assert
        Assert.IsType<ConsoleLogger>(logger);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterJsonLogger_WhenJsonFlagIsPresent()
    {
        // Arrange
        var args = new string[] { "--json" };
        
        // Act
        using var host = AppHost.CreateHost(args);
        var logger = host.Services.GetRequiredService<ILogger>();

        // Assert
        Assert.IsType<JsonLogger>(logger);
    }

    [Fact]
    public void ConfigureServices_ShouldRegisterJsonLogger_WhenJsonFlagIsPresentWithOtherArgs()
    {
        // Arrange
        var args = new string[] { "--dry-run", "--json", "--debug" };
        
        // Act
        using var host = AppHost.CreateHost(args);
        var logger = host.Services.GetRequiredService<ILogger>();

        // Assert
        Assert.IsType<JsonLogger>(logger);
    }
}
