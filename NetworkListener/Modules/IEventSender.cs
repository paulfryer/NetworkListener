using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Nancy;

namespace NetworkListener.Modules
{
    public interface IEventSender
    {
        Task SendEvent(EventData eventData, string content, Request request);
    }
}