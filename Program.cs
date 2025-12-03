using System.CommandLine;
using System.CommandLine.Parsing;
using WinHome;
using WinHome.Models;
using YamlDotNet.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. Config File Option
        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Path to the YAML configuration file",
            DefaultValueFactory = _ => new FileInfo("config.yaml") 
        };

        // 2. Dry Run Option
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Preview changes without applying them",
            DefaultValueFactory = _ => false
        };
        dryRunOption.Aliases.Add("-d");

        // 3. Profile Option (e.g. --profile work)
        var profileOption = new Option<string?>("--profile")
        {
            Description = "Activate a specific profile (e.g. work, personal)",
            DefaultValueFactory = _ => null
        };
        profileOption.Aliases.Add("-p");

        // Setup Root Command
        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
        rootCommand.Options.Add(configOption);
        rootCommand.Options.Add(dryRunOption);
        rootCommand.Options.Add(profileOption);

        rootCommand.SetAction((ParseResult result) => 
        {
            FileInfo file = result.GetValue(configOption)!;
            bool dryRun = result.GetValue(dryRunOption);
            string? profile = result.GetValue(profileOption);
            
            RunApp(file, dryRun, profile);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    static void RunApp(FileInfo file, bool dryRun, string? profile)
    {
        if (!file.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error] Config file not found at: {file.FullName}");
            Console.ResetColor();
            return;
        }

        try 
        {
            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("--- DRY RUN MODE: No changes will be made ---");
                Console.ResetColor();
            }

            Console.WriteLine($"Reading config from: {file.Name}");

            var yamlText = File.ReadAllText(file.FullName);
            
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties() // Makes it robust against minor schema mismatches
                .Build();

            var config = deserializer.Deserialize<Configuration>(yamlText);
            
            // Initialize and Run Engine
            var engine = new Engine();
            engine.Run(config, dryRun, profile);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Critical Error] {ex.Message}");
            Console.ResetColor();
        }
    }
}