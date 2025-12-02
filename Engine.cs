using WinHome.Models;
using WinHome.Services;

namespace WinHome
{
    public class Engine
    {
        private readonly WingetService _winget;

        public Engine()
        {
            _winget = new WingetService();
        }

        public void Run(Configuration config)
        {
            Console.WriteLine($"--- WinHome v{config.Version} ---");
            
            // check if we have any apps to install
            if (config.Apps.Any())
            {
                Console.WriteLine("--- Processing Applications ---");
                foreach (var app in config.Apps)
                {
                    // This calls the code you wrote in Step 2!
                    _winget.EnsureInstalled(app.Id);
                }
            }
            else 
            {
                Console.WriteLine("No apps found in configuration.");
            }
        }
    }
}