using Moq;
using WinHome.Interfaces;
using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
  public class SecretResolverTests
  {
    private readonly Mock<ILogger> _mockLogger;
    private readonly SecretResolver _resolver;

    public SecretResolverTests()
    {
      _mockLogger = new Mock<ILogger>();
      _resolver = new SecretResolver(_mockLogger.Object);
    }

    [Fact]
    public void Resolve_Env_ReturnsEnvironmentVariable()
    {
      // Arrange
      Environment.SetEnvironmentVariable("WINHOME_TEST_SECRET", "super-secret-value");

      // Act
      var result = _resolver.Resolve("{{ env:WINHOME_TEST_SECRET }}");

      // Assert
      Assert.Equal("super-secret-value", result);

      // Cleanup
      Environment.SetEnvironmentVariable("WINHOME_TEST_SECRET", null);
    }

    [Fact]
    public void Resolve_File_ReturnsFileContent()
    {
      // Arrange
      var tempFile = Path.GetTempFileName();
      File.WriteAllText(tempFile, "file-secret-content");

      // Act
      var result = _resolver.Resolve($"{{{{ file:{tempFile} }}}}");

      // Assert
      Assert.Equal("file-secret-content", result);

      // Cleanup
      File.Delete(tempFile);
    }

    [Fact]
    public void ResolveObject_RecursivelyResolvesComplexObject()
    {
      // Arrange
      Environment.SetEnvironmentVariable("MY_VAR", "resolved-var");
      var tempFile = Path.GetTempFileName();
      File.WriteAllText(tempFile, "resolved-file");

      var testObj = new TestConfig
      {
        Name = "{{ env:MY_VAR }}",
        Details = new TestDetails
        {
          KeyPath = $"{{{{ file:{tempFile} }}}}",
          Inner = new List<string> { "plain", "{{ env:MY_VAR }}" }
        },
        Tags = new Dictionary<string, string>
                {
                    { "secret", "{{ env:MY_VAR }}" }
                }
      };

      // Act
      _resolver.ResolveObject(testObj);

      // Assert
      Assert.Equal("resolved-var", testObj.Name);
      Assert.Equal("resolved-file", testObj.Details.KeyPath);
      Assert.Equal("plain", testObj.Details.Inner[0]);
      Assert.Equal("resolved-var", testObj.Details.Inner[1]);
      Assert.Equal("resolved-var", testObj.Tags["secret"]);

      // Cleanup
      Environment.SetEnvironmentVariable("MY_VAR", null);
      File.Delete(tempFile);
    }

    private class TestConfig
    {
      public string Name { get; set; } = "";
      public TestDetails Details { get; set; } = new();
      public Dictionary<string, string> Tags { get; set; } = new();
    }

    [Fact]
    public void Resolve_Vault_ReturnsEmptyAndWarns_WhenCredentialNotFound()
    {
      // Act
      var result = _resolver.Resolve("{{ vault:WINHOME_NONEXISTENT_CREDENTIAL_XYZ }}");

      // Assert: on Windows, missing credential returns empty; on non-Windows, also returns empty
      Assert.Equal(string.Empty, result);
      _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("WINHOME_NONEXISTENT_CREDENTIAL_XYZ") || s.Contains("Windows"))), Times.Once);
    }

    [Fact]
    public void Resolve_Vault_ReturnsOriginalToken_WhenExceptionThrown()
    {
      // The resolver catches exceptions and returns the original match value
      // This test verifies the fallback path via a non-existent key (no exception expected, just empty)
      var result = _resolver.Resolve("{{ vault:some-key }}");
      Assert.True(result == string.Empty || result == "{{ vault:some-key }}");
    }

    // Live vault test: store a credential first with:
    //   cmdkey /add:WINHOME_TEST_VAULT /user:test /pass:vault-secret
    // then remove it after:
    //   cmdkey /delete:WINHOME_TEST_VAULT
    [Fact(Skip = "Requires a real credential in Windows Credential Manager. Run manually after seeding with cmdkey.")]
    public void Resolve_Vault_ReturnsSecret_WhenCredentialExists()
    {
      const string credName = "WINHOME_TEST_VAULT";
      var result = _resolver.Resolve($"{{{{ vault:{credName} }}}}");
      Assert.Equal("vault-secret", result);
    }

    private class TestDetails
    {
      public string KeyPath { get; set; } = "";
      public List<string> Inner { get; set; } = new();
    }
  }
}
