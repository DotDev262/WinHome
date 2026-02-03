using WinHome.Services.System;
using Xunit;

namespace WinHome.Tests
{
    public class ConfigValidatorTests
    {
        private readonly ConfigValidator _validator;

        public ConfigValidatorTests()
        {
            _validator = new ConfigValidator();
        }

        [Fact]
        public void Validate_ReturnsTrue_ForValidConfig()
        {
            // Arrange
            string validYaml = @"
apps:
  - id: vscode
    manager: winget
dotfiles:
  - repo: https://github.com/user/dotfiles.git
    target: ~/.config
";
            // Act
            var result = _validator.Validate(validYaml);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_ReturnsFalse_ForInvalidType()
        {
            // Arrange
            string invalidYaml = @"
apps:
  - id: vscode
    manager: [1, 2]  # Should be string, is array
";
            // Act
            var result = _validator.Validate(invalidYaml);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Validate_ReturnsFalse_ForInvalidYaml()
        {
            // Arrange
            string invalidYaml = @"
apps:
  - id: vscode
    manager: winget
  [invalid syntax]
";
            // Act
            var result = _validator.Validate(invalidYaml);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.StartsWith("YAML Parsing Error"));
        }
    }
}
