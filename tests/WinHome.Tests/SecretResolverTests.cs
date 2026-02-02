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

        private class TestDetails
        {
            public string KeyPath { get; set; } = "";
            public List<string> Inner { get; set; } = new();
        }
    }
}
