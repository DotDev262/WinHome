using WinHome.Interfaces;
using System.ServiceProcess;
using System.Linq;

namespace WinHome.Services.System
{
    public class ServiceControllerWrapper : IServiceControllerWrapper
    {
        public bool ServiceExists(string serviceName)
        {
            return ServiceController.GetServices().Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        public ServiceControllerStatus GetServiceStatus(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            return service.Status;
        }

        public void StartService(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            if (service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running);
            }
        }

        public void StopService(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }
    }
}