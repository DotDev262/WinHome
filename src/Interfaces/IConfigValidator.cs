namespace WinHome.Interfaces;

/// <summary>
/// Defines a contract for validating YAML configuration files.
/// </summary>
public interface IConfigValidator
{
    /// <summary>
    /// Validates the provided YAML text and returns the result.
    /// </summary>
    /// <param name="yamlText">The raw YAML text content to validate.</param>
    /// <returns>A tuple with a boolean indicating validity and a list of error messages.</returns>
    (bool IsValid, List<string> Errors) Validate(string yamlText);
}