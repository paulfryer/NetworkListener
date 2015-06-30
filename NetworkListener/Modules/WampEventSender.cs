using System;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using Nancy;
using Newtonsoft.Json;
using Wamplash;

namespace NetworkListener.Modules
{
    public class WampEventSender : IEventSender
    {
        private static readonly WampClient WampClient = new WampClient();
        private static bool wampClientConnected;
        private readonly string topic;


        public WampEventSender()
        {
            topic = "io.crossbar.demo.pubsub.082880";
            if (!wampClientConnected)
            {
                WampClient.Connect(new Uri("ws://wamplash.azurewebsites.net/ws"), "defaultRealm");
                //WampClient.Subscribe(topic).Wait();
                wampClientConnected = true;

                //for (int i =0; i<100;i++)
                //WampClient.Publish(topic, JsonConvert.DeserializeObject<dynamic>("[\"" + Thread.CurrentThread.ManagedThreadId + " - " + i + " TESTTEST\"]"));
            }
        }

        public async Task SendEvent(EventData eventData, string content, Request request)
        {
            if (content.Trim().StartsWith("<") && content.Trim().EndsWith(">"))
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(content);
                    var bodyJson = JsonConvert.SerializeXmlNode(doc);
                    var headersJson = JsonConvert.SerializeObject(request.Headers);
                    var eventJson = "[" + headersJson + ", " + bodyJson + "]";
                    await WampClient.Publish(topic, eventJson);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}