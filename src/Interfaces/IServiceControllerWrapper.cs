using System.ServiceProcess;

namespace WinHome.Interfaces
{
  /// <summary>Abstraction wrapping Windows Service Controller operations.</summary>
  public interface IServiceControllerWrapper
  {
    /// <summary>Returns <c>true</c> if a Windows service with the given name exists.</summary>
    bool ServiceExists(string serviceName);
    /// <summary>Gets the current status of a Windows service.</summary>
    ServiceControllerStatus GetServiceStatus(string serviceName);
    /// <summary>Starts the specified Windows service.</summary>
    void StartService(string serviceName);
    /// <summary>Stops the specified Windows service.</summary>
    void StopService(string serviceName);
  }
}
