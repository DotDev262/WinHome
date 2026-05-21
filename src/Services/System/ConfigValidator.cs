using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using WinHome.Interfaces;
using WinHome.Models;
using YamlDotNet.Serialization;

namespace WinHome.Services.System;

/// <summary>
/// Provides a platform validation engine that parses raw text configuration streams into temporary objects, 
/// serializes them to standard JSON formats, and evaluates structural metadata properties against compiled schema matrices.
/// </summary>
public class ConfigValidator : IConfigValidator
{
    private readonly JsonSchema _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigValidator"/> class, dynamically compiling a structural 
    /// validation schema from the strong types specified by the root <see cref="Configuration"/> graph blueprint layout.
    /// </summary>
    public ConfigValidator()
    {
        _schema = new JsonSchemaBuilder()
            .FromType<Configuration>()
            .Build();
    }

    /// <summary>
    /// Validates an incoming textual configuration manifest layout string by translating raw structures 
    /// through intermediate JSON layers to run evaluation matches.
    /// </summary>
    /// <param name="yamlText">The raw input text content string representing the target file layout configuration format to assert.</param>
    /// <returns>A strongly bound evaluation tuple indicating if the manifest layout matches compliance standards (<c>IsValid</c>) along with any parsing anomalies or formatting error collection strings (<c>Errors</c>).</returns>
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