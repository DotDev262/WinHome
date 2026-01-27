using WinHome.Interfaces;
using WinHome.Models;
using Microsoft.Extensions.Logging;
using System.ServiceProcess; // Required for ServiceControllerStatus

namespace WinHome.Services.System
{
    public class WindowsServiceManager : IWindowsServiceManager
    {
        private readonly ILogger<WindowsServiceManager> _logger;
        private readonly IProcessRunner _processRunner;
        private readonly IServiceControllerWrapper _serviceControllerWrapper;

        public WindowsServiceManager(
            ILogger<WindowsServiceManager> logger,
            IProcessRunner processRunner,
            IServiceControllerWrapper serviceControllerWrapper)
        {
            _logger = logger;
            _processRunner = processRunner;
            _serviceControllerWrapper = serviceControllerWrapper;
        }

        public void Apply(WindowsServiceConfig service, bool dryRun)
        {
            _logger.LogInformation($"Processing service: {service.Name}");

            if (!_serviceControllerWrapper.ServiceExists(service.Name))
            {
                _logger.LogWarning($"Service '{service.Name}' not found. Skipping.");
                return;
            }

            if (service.StartupType != null)
            {
                SetStartupType(service.Name, service.StartupType, dryRun);
            }

            if (service.State != null)
            {
                SetServiceState(service.Name, service.State, dryRun);
            }
        }

        private void SetStartupType(string serviceName, string startupType, bool dryRun)
        {
            _logger.LogInformation($"{(dryRun ? "[Dry Run] " : "")}Setting startup type of service '{serviceName}' to '{startupType}'");
            // sc.exe config "serviceName" start= auto/demand/disabled
            var args = $"config \"{serviceName}\" start= {startupType}";
            if (!_processRunner.RunCommand("sc.exe", args, dryRun))
            {
                _logger.LogError($"Failed to set startup type for service '{serviceName}'.");
            }
        }

        private void SetServiceState(string serviceName, string state, bool dryRun)
        {
            var currentStatus = _serviceControllerWrapper.GetServiceStatus(serviceName);
            _logger.LogInformation($"Service '{serviceName}' current status: {currentStatus}");

            string action = "";
            bool shouldAct = false;

            if (state.Equals("running", StringComparison.OrdinalIgnoreCase))
            {
                if (currentStatus != ServiceControllerStatus.Running)
                {
                    action = "start";
                    shouldAct = true;
                }
            }
            else if (state.Equals("stopped", StringComparison.OrdinalIgnoreCase))
            {
                if (currentStatus != ServiceControllerStatus.Stopped)
                {
                    action = "stop";
                    shouldAct = true;
                }
            }
            else
            {
                _logger.LogWarning($"Unknown service state '{state}' for service '{serviceName}'. Skipping state management.");
                return;
            }

            if (!shouldAct)
            {
                _logger.LogInformation($"Service '{serviceName}' is already in the desired state ('{state}'). No action needed.");
                return;
            }

            _logger.LogInformation($"{(dryRun ? "[Dry Run] " : "")}{action.Capitalize()}ing service '{serviceName}'...");
            if (!dryRun)
            {
                try
                {
                    if (action == "start")
                    {
                        _serviceControllerWrapper.StartService(serviceName);
                    }
                    else if (action == "stop")
                    {
                        _serviceControllerWrapper.StopService(serviceName);
                    }
                    _logger.LogInformation($"Successfully {action}ed service '{serviceName}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to {action} service '{serviceName}': {ex.Message}");
                }
            }
        }
    }

    public static class StringExtensions
    {
        public static string Capitalize(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
