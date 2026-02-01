namespace WinHome.Interfaces;

public interface IConfigValidator
{
    (bool IsValid, List<string> Errors) Validate(string yamlText);
}
