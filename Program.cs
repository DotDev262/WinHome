using System.CommandLine;
using System.CommandLine.Parsing;
using WinHome;
using WinHome.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. Define the Option
        // we use the Object Initializer syntax { ... } as shown in the docs
        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Path to the YAML configuration file",
            // FIX: It is a Property now, not a function call
            DefaultValueFactory = _ => new FileInfo("config.yaml") 
        };

        // 2. Define the Root Command
        var rootCommand = new RootCommand("WinHome: Windows Setup Tool");
        
        // 3. Add the option to the command
        rootCommand.Options.Add(configOption);

        // 4. Define the Action
        rootCommand.SetAction((ParseResult result) => 
        {
            // Extract the value using the specific Option object
            FileInfo file = result.GetValue(configOption)!;
            Execute(file);
        });

        // 5. Run it
        return await rootCommand.Parse(args).InvokeAsync();
    }

    static void Execute(FileInfo file)
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
            Console.WriteLine($"Reading config from: {file.Name}");

            var yamlText = File.ReadAllText(file.FullName);
            
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<Configuration>(yamlText);
            
            var engine = new Engine();
            engine.Run(config);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Critical Error] {ex.Message}");
            Console.ResetColor();
        }
    }
}