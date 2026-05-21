using System.ServiceProcess;

namespace WinHome.Interfaces
{
    /// <summary>
    /// Defines a contract for querying and controlling Windows services.
    /// </summary>
    public interface IServiceControllerWrapper
    {
        /// <summary>
        /// Determines whether a Windows service with the given name exists.
        /// </summary>
        /// <param name="serviceName">The name of the service to check.</param>
        /// <returns><c>true</c> if the service exists; otherwise <c>false</c>.</returns>
        bool ServiceExists(string serviceName);

        /// <summary>
        /// Gets the current status of the specified Windows service.
        /// </summary>
        /// <param name="serviceName">The name of the service to query.</param>
        /// <returns>The current <see cref="ServiceControllerStatus"/> of the service.</returns>
        ServiceControllerStatus GetServiceStatus(string serviceName);

        /// <summary>
        /// Starts the specified Windows service.
        /// </summary>
        /// <param name="serviceName">The name of the service to start.</param>
        void StartService(string serviceName);

        /// <summary>
        /// Stops the specified Windows service.
        /// </summary>
        /// <param name="serviceName">The name of the service to stop.</param>
        void StopService(string serviceName);
    }
}