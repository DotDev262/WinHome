using System;
using System.ServiceProcess;
using System.Linq;
using WinHome.Interfaces;

namespace WinHome.Services.System
{
    /// <summary>
    /// Implements a wrapper around the native <see cref="ServiceController"/>, providing 
    /// abstracted methods to query, start, and stop Windows system services safely.
    /// </summary>
    public class ServiceControllerWrapper : IServiceControllerWrapper
    {
        /// <summary>
        /// Determines whether a service with the specified name is currently installed on the host system.
        /// </summary>
        /// <param name="serviceName">The case-insensitive name identifier of the service to locate.</param>
        /// <returns><c>true</c> if the service is found; otherwise, <c>false</c>.</returns>
        public bool ServiceExists(string serviceName)
        {
            return ServiceController.GetServices()
                .Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retrieves the current operational status of the specified Windows service.
        /// </summary>
        /// <param name="serviceName">The name of the service to query.</param>
        /// <returns>A <see cref="ServiceControllerStatus"/> representing the current state (e.g., Running, Stopped).</returns>
        public ServiceControllerStatus GetServiceStatus(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            return service.Status;
        }

        /// <summary>
        /// Initiates a start command for the specified service and synchronously awaits the transition to the 'Running' state.
        /// </summary>
        /// <param name="serviceName">The name of the service to start.</param>
        public void StartService(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            if (service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            }
        }

        /// <summary>
        /// Initiates a stop command for the specified service and synchronously awaits the transition to the 'Stopped' state.
        /// </summary>
        /// <param name="serviceName">The name of the service to stop.</param>
        public void StopService(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }
        }
    }
}