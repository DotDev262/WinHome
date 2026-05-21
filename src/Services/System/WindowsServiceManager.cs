using System;
using System.ServiceProcess;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Manages Windows Service configurations including startup types and operational states.
    /// Acts as an abstraction layer over the native Service Control Manager (SCM).
    /// </summary>
    [SupportedOSPlatform("windows")]
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

        /// <summary>
        /// Applies the desired configuration to a service.
        /// </summary>
        public void Apply(WindowsServiceConfig service, bool dryRun)
        {
            _logger.LogInformation($"[Service] Processing service: {service.Name}");

            if (!_serviceControllerWrapper.ServiceExists(service.Name))
            {
                _logger.LogWarning($"[Service] Service '{service.Name}' not found on host. Skipping.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(service.StartupType))
            {
                SetStartupType(service.Name, service.StartupType, dryRun);
            }

            if (!string.IsNullOrWhiteSpace(service.State))
            {
                SetServiceState(service.Name, service.State, dryRun);
            }
        }

        private void SetStartupType(string serviceName, string startupType, bool dryRun)
        {
            string prefix = dryRun ? "[Dry Run] " : "";
            _logger.LogInformation($"{prefix}Setting startup type of '{serviceName}' to '{startupType}'");
            
            // Note: 'sc.exe' syntax is strictly 'start= auto' (note the required space)
            var args = $"config \"{serviceName}\" start= {startupType}";
            
            if (!_processRunner.RunCommand("sc.exe", args, dryRun))
            {
                _logger.LogError($"[Service] Failed to set startup type to '{startupType}' for service '{serviceName}'.");
            }
        }

        private void SetServiceState(string serviceName, string state, bool dryRun)
        {
            var currentStatus = _serviceControllerWrapper.GetServiceStatus(serviceName);
            
            bool isRunning = currentStatus == ServiceControllerStatus.Running;
            bool isStopped = currentStatus == ServiceControllerStatus.Stopped;

            string action = state.ToLower() switch
            {
                "running" when !isRunning => "start",
                "stopped" when !isStopped => "stop",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(action))
            {
                _logger.LogInformation($"[Service] '{serviceName}' is already in the desired state ('{state}'). No action needed.");
                return;
            }

            _logger.LogInformation($"{(dryRun ? "[Dry Run] " : "")}{action.Capitalize()}ing service '{serviceName}'...");

            if (!dryRun)
            {
                try
                {
                    if (action == "start") _serviceControllerWrapper.StartService(serviceName);
                    else _serviceControllerWrapper.StopService(serviceName);

                    _logger.LogSuccess($"[Service] Successfully {action}ed service '{serviceName}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Service] Failed to {action} service '{serviceName}': {ex.Message}");
                }
            }
        }
    }

    public static class StringExtensions
    {
        public static string Capitalize(this string input) =>
            string.IsNullOrEmpty(input) ? input : char.ToUpper(input[0]) + input.Substring(1);
    }
}