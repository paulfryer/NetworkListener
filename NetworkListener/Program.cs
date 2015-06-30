using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
using NetworkListener.Devices;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace NetworkListener
{
    public class Program
    {
        private static NancyHost nancyHost;
        public static Uri WebServerRoot;
        private static ServiceSearcher serviceSearcher;
        public static TimeSpan SubscriptionTimeSpan = TimeSpan.FromMinutes(15);
        public static List<WebSocket> WebSockets = new List<WebSocket>();

        public static string GetIp()
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList.First(o => o.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
        }

        public static void Main()
        {
            var host = GetIp();
            const string port = "80";
            WebServerRoot = new Uri(string.Format("http://{0}:{1}", host, port));
            nancyHost = new NancyHost(WebServerRoot);
            nancyHost.Start();
            serviceSearcher = new ServiceSearcher(SubscriptionTimeSpan);
            serviceSearcher.ServiceDiscovered += OnServiceDiscovered;

            StartWebSocketServer();

            Console.ReadKey();
            nancyHost.Stop();
            //nancyHost.Dispose();
        }


        private static void StartWebSocketServer()
        {
            var cancellation = new CancellationTokenSource();

            var endpoint = new IPEndPoint(IPAddress.Any, 8005);
            var server = new WebSocketListener(endpoint);
            var rfc6455 = new WebSocketFactoryRfc6455(server);
            server.Standards.RegisterStandard(rfc6455);
            server.Start();

            Log("Websocket Server started at " + endpoint);

            var task = Task.Run(() => AcceptWebSocketClientsAsync(server, cancellation.Token), cancellation.Token);

            Console.ReadKey(true);
            Log("Server stoping");
            cancellation.Cancel();
            task.Wait(cancellation.Token);
            Console.ReadKey(true);
        }

        private static async Task AcceptWebSocketClientsAsync(WebSocketListener server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var ws = await server.AcceptWebSocketAsync(token).ConfigureAwait(false);
                    if (ws != null)
                        await Task.Run(() => HandleConnectionAsync(ws, token), token);
                }
                catch (Exception aex)
                {
                    Log("Error Accepting clients: " + aex.GetBaseException().Message);
                }
            }
            Log("Server Stop accepting clients");
        }

        private static void Log(string message)
        {
            Console.WriteLine("+++ " + message);
        }

        private static async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
        {
            try
            {
                WebSockets.Add(ws);
            }
            catch (Exception aex)
            {
                Log("Error Handling connection: " + aex.GetBaseException().Message);
                try
                {
                    ws.Close();
                }
                catch
                {
                    Console.WriteLine("Error trying to close websocket connection.");
                }
            }
        }

        private static void OnServiceDiscovered(ServiceDescription serviceDescription)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Found NEW service: " + serviceDescription.Location);

                if (serviceDescription.Location.IsAbsoluteUri)
                {
                    var device = DeviceParser.Load(serviceDescription.Location).Result;


                    if (device != null)
                    {
                        Console.WriteLine("Device Type: " + device.DeviceType);
                        Console.WriteLine("Firendly Name: " + device.FriendlyName);
                        Console.WriteLine("Manufacturer: " + device.Manufacturer.Name);
                        if (device.Manufacturer.Url != null)
                            Console.WriteLine("Manufacturer Link: " + device.Manufacturer.Url.AbsoluteUri);

                        if (device.Services != null)
                            foreach (var service in device.Services)
                            {
                                Console.WriteLine(service.EventSubscriptionUrl.AbsoluteUri);
                            }


                        if (device.Services != null)
                            foreach (var service in device.Services)
                            {
                                var subscriptionUrl = service.EventSubscriptionUrl;
                                var notificationUrl = new Uri("http://" + GetIp() + ":80/notify");


                                //if (subscriptionUrl.AbsoluteUri.EndsWith("/MediaRenderer/RenderingControl/Event"))
                                //{
                                Console.WriteLine("Using Server: " + serviceDescription.Server);
                                Console.WriteLine("Using subscriptionUrl: " + subscriptionUrl.AbsoluteUri);
                                Console.WriteLine("Using notify url: " + notificationUrl.AbsoluteUri);
                                serviceDescription.Subscribe(subscriptionUrl, notificationUrl, SubscriptionTimeSpan);
                                // }


                                if (device.Devices != null)
                                {
                                    Console.WriteLine("Found sub devices, count: " + device.Devices.Count);
                                    foreach (var subDevice in device.Devices)
                                    {
                                        if (subDevice.Services != null)
                                            foreach (var subDeviceService in subDevice.Services)
                                            {
                                                Console.WriteLine("Subdevice service: " +
                                                                  subDeviceService.EventSubscriptionUrl.AbsoluteUri);
                                                //if (subDeviceService.EventSubscriptionUrl.AbsoluteUri.EndsWith("/MediaRenderer/RenderingControl/Event"))
                                                //{
                                                Console.WriteLine("SUBSCRIBING TO SUB SERVICE: " +
                                                                  subDeviceService.EventSubscriptionUrl.AbsoluteUri);
                                                serviceDescription.Subscribe(subDeviceService.EventSubscriptionUrl,
                                                    notificationUrl,
                                                    SubscriptionTimeSpan);
                                                //}
                                            }
                                    }
                                }
                            }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR: " + exception);
            }
        }
    }
}