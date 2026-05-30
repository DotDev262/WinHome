namespace WinHome.Interfaces;

/// <summary>Validates YAML configuration text and returns validation errors.</summary>
public interface IConfigValidator
{
  /// <summary>Validates the provided YAML text and returns a tuple indicating validity and any errors.</summary>
  /// <param name="yamlText">Raw YAML string to validate.</param>
  /// <returns>A tuple with <c>IsValid</c> indicating success and <c>Errors</c> listing validation messages.</returns>
  (bool IsValid, List<string> Errors) Validate(string yamlText);
}
