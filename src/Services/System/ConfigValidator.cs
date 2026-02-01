using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Services.System;

public class ConfigValidator : IConfigValidator
{
    private readonly JsonSchema _schema;

    public ConfigValidator()
    {
        _schema = new JsonSchemaBuilder()
            .FromType<Configuration>()
            .Build();
    }

    public (bool IsValid, List<string> Errors) Validate(string yamlText)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize<object>(yamlText);

            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();
            
            string jsonText = serializer.Serialize(yamlObject);
            
            using var jsonDoc = JsonDocument.Parse(jsonText);
            var results = _schema.Evaluate(jsonDoc.RootElement, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            if (results.IsValid)
            {
                return (true, new List<string>());
            }

            var errors = (results.Details ?? Enumerable.Empty<EvaluationResults>())
                .Where(x => !x.IsValid && x.Errors != null)
                .SelectMany(x => x.Errors!.Values.Select(v => $"{x.InstanceLocation}: {v}"))
                .ToList();

            if (!errors.Any())
            {
                errors.Add("Unknown validation error.");
            }

            return (false, errors);
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"YAML Parsing Error: {ex.Message}" });
        }
    }
}
