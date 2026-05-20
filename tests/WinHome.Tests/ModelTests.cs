using System.Text.Json;
using YamlDotNet.Serialization;
using WinHome.Models;

namespace WinHome.Tests;

public class ModelTests
{
    #region AppConfig Tests

    [Fact]
    public void AppConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.Equal(string.Empty, config.Id);
        Assert.Null(config.Source);
        Assert.Equal("winget", config.Manager);
        Assert.Null(config.Version);
        Assert.Null(config.Params);
    }

    [Fact]
    public void AppConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new AppConfig
        {
            Id = "Microsoft.PowerToys",
            Source = "winget",
            Manager = "winget",
            Version = "0.80.0",
            Params = "--silent --force"
        };

        // Act
        var jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Source, deserialized.Source);
        Assert.Equal(original.Manager, deserialized.Manager);
        Assert.Equal(original.Version, deserialized.Version);
        Assert.Equal(original.Params, deserialized.Params);
    }

    [Fact]
    public void AppConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new AppConfig
        {
            Id = "neovim",
            Source = "scoop",
            Manager = "scoop",
            Version = "0.10.0",
            Params = "--global"
        };

        // Act
        var serializer = new SerializerBuilder().Build();
        var yamlString = serializer.Serialize(original);
        var deserializer = new DeserializerBuilder().Build();
        var deserialized = deserializer.Deserialize<AppConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Source, deserialized.Source);
        Assert.Equal(original.Manager, deserialized.Manager);
        Assert.Equal(original.Version, deserialized.Version);
        Assert.Equal(original.Params, deserialized.Params);
    }

    #endregion

    #region WslDistroConfig Tests

    [Fact]
    public void WslDistroConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new WslDistroConfig();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Null(config.SetupScript);
    }

    [Fact]
    public void WslDistroConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new WslDistroConfig
        {
            Name = "Ubuntu-22.04",
            SetupScript = "~/setup.sh"
        };

        // Act
        var jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WslDistroConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.SetupScript, deserialized.SetupScript);
    }

    [Fact]
    public void WslDistroConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new WslDistroConfig
        {
            Name = "Debian",
            SetupScript = "/opt/setup.sh"
        };

        var serializer = new SerializerBuilder().Build();
        var deserializer = new DeserializerBuilder().Build();

        // Act
        var yamlString = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<WslDistroConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.SetupScript, deserialized.SetupScript);
    }

    #endregion

    #region WindowsServiceConfig Tests

    [Fact]
    public void WindowsServiceConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new WindowsServiceConfig();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal("running", config.State);
        Assert.Null(config.StartupType);
    }

    [Fact]
    public void WindowsServiceConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new WindowsServiceConfig
        {
            Name = "Spooler",
            State = "stopped",
            StartupType = "manual"
        };

        // Act
        var jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WindowsServiceConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.State, deserialized.State);
        Assert.Equal(original.StartupType, deserialized.StartupType);
    }

    [Fact]
    public void WindowsServiceConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new WindowsServiceConfig
        {
            Name = "W32Time",
            State = "running",
            StartupType = "automatic"
        };

        var serializer = new SerializerBuilder().Build();
        var deserializer = new DeserializerBuilder().Build();

        // Act
        var yamlString = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<WindowsServiceConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.State, deserialized.State);
        Assert.Equal(original.StartupType, deserialized.StartupType);
    }

    #endregion

    #region DotfileConfig Tests

    [Fact]
    public void DotfileConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new DotfileConfig();

        // Assert
        Assert.Equal(string.Empty, config.Src);
        Assert.Equal(string.Empty, config.Target);
    }

    [Fact]
    public void DotfileConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new DotfileConfig
        {
            Src = "./dotfiles/.gitconfig",
            Target = "~/.gitconfig"
        };

        // Act
        var jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DotfileConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Src, deserialized.Src);
        Assert.Equal(original.Target, deserialized.Target);
    }

    [Fact]
    public void DotfileConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new DotfileConfig
        {
            Src = "./dotfiles/.vimrc",
            Target = "~/.vimrc"
        };

        var serializer = new SerializerBuilder().Build();
        var deserializer = new DeserializerBuilder().Build();

        // Act
        var yamlString = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<DotfileConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Src, deserialized.Src);
        Assert.Equal(original.Target, deserialized.Target);
    }

    #endregion

    #region ProfileConfig Tests

    [Fact]
    public void ProfileConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new ProfileConfig();

        // Assert
        Assert.Null(config.Git);
    }

    [Fact]
    public void ProfileConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new ProfileConfig
        {
            Git = new GitConfig
            {
                UserName = "Profile User",
                UserEmail = "profile@example.com",
                CommitGpgSign = true,
                Settings = new Dictionary<string, string>
                {
                    { "core.editor", "code --wait" }
                }
            }
        };

        // Act
        var jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ProfileConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Git);
        Assert.Equal(original.Git.UserName, deserialized.Git.UserName);
        Assert.Equal(original.Git.UserEmail, deserialized.Git.UserEmail);
        Assert.Equal(original.Git.CommitGpgSign, deserialized.Git.CommitGpgSign);
        Assert.Equal(original.Git.Settings["core.editor"], deserialized.Git.Settings["core.editor"]);
    }

    [Fact]
    public void ProfileConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new ProfileConfig
        {
            Git = new GitConfig
            {
                UserName = "Yaml User",
                UserEmail = "yaml@example.com",
                SigningKey = "ABC123",
                CommitGpgSign = false,
                Settings = new Dictionary<string, string>
                {
                    { "core.autocrlf", "true" }
                }
            }
        };

        var serializer = new SerializerBuilder().Build();
        var deserializer = new DeserializerBuilder().Build();

        // Act
        var yamlString = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<ProfileConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Git);
        Assert.Equal(original.Git.UserName, deserialized.Git.UserName);
        Assert.Equal(original.Git.UserEmail, deserialized.Git.UserEmail);
        Assert.Equal(original.Git.SigningKey, deserialized.Git.SigningKey);
        Assert.Equal(original.Git.CommitGpgSign, deserialized.Git.CommitGpgSign);
        Assert.Equal(original.Git.Settings["core.autocrlf"], deserialized.Git.Settings["core.autocrlf"]);
    }

    #endregion

    #region GitConfig Tests

    [Fact]
    public void GitConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new GitConfig();

        // Assert
        Assert.Null(config.UserName);
        Assert.Null(config.UserEmail);
        Assert.Null(config.SigningKey);
        Assert.Null(config.CommitGpgSign);
        Assert.NotNull(config.Settings);
        Assert.Empty(config.Settings);
    }

    [Fact]
    public void GitConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new GitConfig
        {
            UserName = "Dev Explorer",
            UserEmail = "dev@example.com",
            SigningKey = "ABC123XYZ",
            CommitGpgSign = true,
            Settings = new Dictionary<string, string>
            {
                { "core.editor", "code --wait" },
                { "init.defaultBranch", "main" }
            }
        };

        // Act
        string jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<GitConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.UserName, deserialized.UserName);
        Assert.Equal(original.UserEmail, deserialized.UserEmail);
        Assert.Equal(original.SigningKey, deserialized.SigningKey);
        Assert.Equal(original.CommitGpgSign, deserialized.CommitGpgSign);

        // Assert dictionary values
        Assert.NotNull(deserialized.Settings);
        Assert.Equal(original.Settings["core.editor"], deserialized.Settings["core.editor"]);
        Assert.Equal(original.Settings["init.defaultBranch"], deserialized.Settings["init.defaultBranch"]);

    }

    [Fact]
    public void GitConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new GitConfig
        {
            UserName = "Dev Explorer",
            UserEmail = "dev@example.com",
            SigningKey = "GPG9876",
            CommitGpgSign = false,
            Settings = new Dictionary<string, string>
            {
                { "core.autocrlf", "true" }
            }
        };

        var serializer = new SerializerBuilder().Build();
        var deserializer = new DeserializerBuilder().Build();

        // Act
        string yamlString = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<GitConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.UserName, deserialized.UserName);
        Assert.Equal(original.UserEmail, deserialized.UserEmail);
        Assert.Equal(original.SigningKey, deserialized.SigningKey);
        Assert.Equal(original.CommitGpgSign, deserialized.CommitGpgSign);

        Assert.NotNull(deserialized.Settings);
        Assert.Equal(original.Settings["core.autocrlf"], deserialized.Settings["core.autocrlf"]);
    }
    #endregion

    #region WslConfig Tests

    [Fact]
    public void WslConfig_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new WslConfig();

        // Assert
        Assert.Equal(2, config.DefaultVersion);
        Assert.Null(config.DefaultDistro);
        Assert.False(config.Update);

        // List should be initialized but empty
        Assert.NotNull(config.Distros);
        Assert.Empty(config.Distros);
    }

    [Fact]
    public void WslConfig_ShouldRoundTrip_JsonSerialization()
    {
        // Arrange
        var original = new WslConfig
        {
            DefaultVersion = 2,
            DefaultDistro = "Ubuntu",
            Update = true,
            Distros = new List<WslDistroConfig>
            {
                new WslDistroConfig { Name = "Ubuntu-22.04" },
                new WslDistroConfig { Name = "Debian" }
            }
        };

        // Act
        string jsonString = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WslConfig>(jsonString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.DefaultVersion, deserialized.DefaultVersion);
        Assert.Equal(original.DefaultDistro, deserialized.DefaultDistro);
        Assert.Equal(original.Update, deserialized.Update);

        // Verify nested list data
        Assert.NotNull(deserialized.Distros);
        Assert.Equal(original.Distros.Count, deserialized.Distros.Count);
        Assert.Equal(original.Distros[0].Name, deserialized.Distros[0].Name);
        Assert.Equal(original.Distros[1].Name, deserialized.Distros[1].Name);
    }

    [Fact]
    public void WslConfig_ShouldRoundTrip_YamlSerialization()
    {
        // Arrange
        var original = new WslConfig
        {
            DefaultVersion = 1,
            DefaultDistro = "Alpine",
            Update = false,
            Distros = new List<WslDistroConfig>
            {
                new WslDistroConfig { Name = "Alpine" }
            }
        };

        var serializer = new SerializerBuilder().Build();
        var deserializer = new DeserializerBuilder().Build();

        // Act
        string yamlString = serializer.Serialize(original);
        var deserialized = deserializer.Deserialize<WslConfig>(yamlString);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.DefaultVersion, deserialized.DefaultVersion);
        Assert.Equal(original.DefaultDistro, deserialized.DefaultDistro);
        Assert.Equal(original.Update, deserialized.Update);

        Assert.NotNull(deserialized.Distros);
        Assert.Single(deserialized.Distros);
        Assert.Equal(original.Distros[0].Name, deserialized.Distros[0].Name);
    }

    #endregion

}
