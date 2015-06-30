using System.Threading.Tasks;

namespace NetworkListener
{
    public interface IUpnpService
    {
        Task StartSearch();
        Task StopSearch();
    }
}