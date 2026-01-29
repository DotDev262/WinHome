using System.ServiceProcess;

namespace WinHome.Interfaces
{
    public interface IServiceControllerWrapper
    {
        bool ServiceExists(string serviceName);
        ServiceControllerStatus GetServiceStatus(string serviceName);
        void StartService(string serviceName);
        void StopService(string serviceName);
    }
}
