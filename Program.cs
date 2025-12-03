using System.CommandLine;
using System.CommandLine.Parsing;
using WinHome;
using WinHome.Models;
using YamlDotNet.Serialization;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Path to the YAML configuration file",
            DefaultValueFactory = _ => new FileInfo("config.yaml") 
        };

        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Preview changes without applying them",
            DefaultValueFactory = _ => false
        };
        dryRunOption.Aliases.Add("-d");

        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
        rootCommand.Options.Add(configOption);
        rootCommand.Options.Add(dryRunOption);

        rootCommand.SetAction((ParseResult result) => 
        {
            FileInfo file = result.GetValue(configOption)!;
            bool dryRun = result.GetValue(dryRunOption);
            
            RunApp(file, dryRun);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    static void RunApp(FileInfo file, bool dryRun)
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
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<Configuration>(yamlText);
            
            var engine = new Engine();
            engine.Run(config, dryRun);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Critical Error] {ex.Message}");
            Console.ResetColor();
        }
    }
}