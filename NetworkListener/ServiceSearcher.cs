using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkListener
{
    public class ServiceSearcher
    {
        public delegate void ServiceDiscoveredEventHandler(ServiceDescription serviceDescription);

        private readonly TimeSpan searchTimeOut;

        private bool searching;
        private UdpClient udpClient;

        public ServiceSearcher(TimeSpan searchTimeOut)

        {
            this.searchTimeOut = searchTimeOut;
            (new Thread(Search)).Start();
        }

        public event ServiceDiscoveredEventHandler ServiceDiscovered;


        public void Search()
        {
            ISearchResponseParser searchResponseParser = new SimpleSearchResponseParser();

            var sb = new StringBuilder();
            sb.AppendLine("M-SEARCH * HTTP/1.1");
            sb.AppendLine("HOST:239.255.255.250:1900");
            sb.AppendLine("MAN:\"ssdp:discover\"");
            //sb.AppendLine("ST:ssdp:all");
            sb.AppendLine("ST: urn:schemas-upnp-org:device:ZonePlayer:1");
            sb.AppendLine("MX:3");
            sb.AppendLine("");
            var searchString = sb.ToString();
            var data = Encoding.UTF8.GetBytes(searchString);


            udpClient = new UdpClient();
            //udpClient.Connect("239.255.255.250", 1900);

            udpClient.Send(data, data.Length, "239.255.255.250", 1900);
            udpClient.Send(data, data.Length, "255.255.255.255", 1900);

            Console.WriteLine("M-Search sent... \r\n");

            var timeoutTime = DateTime.UtcNow.Add(searchTimeOut);
            searching = true;
            while (searching)
            {
                if (DateTime.UtcNow > timeoutTime)
                {
                    Console.WriteLine("Search Timeout Reached. Starting a new search.");
                    //udpSocket.Close();
                    //udpSocket.Dispose();

                    // Note: this might cause a stack overflow exception because we are recursivley calling this over and over.
                    // should probably refactor this to use an external timer that starts and stops / disposes.
                    Search();
                }

                if (udpClient.Client.Available > 0)
                {
                    var receiveBuffer = new byte[udpClient.Client.Available];
                    var receivedBytes = udpClient.Client.Receive(receiveBuffer, SocketFlags.None);
                    if (receivedBytes > 0)
                    {
                        var characters = Encoding.UTF8.GetChars(receiveBuffer);
                        var s = new string(characters);
                        var serviceDescription = searchResponseParser.Parse(s);
                        if (ServiceDiscovered != null)
                            ServiceDiscovered(serviceDescription);
                    }
                }
                Thread.Sleep(100);
                //UdpSocket.SendTo(Encoding.UTF8.GetBytes(searchString), SocketFlags.None, MulticastEndPoint);
            }
        }
    }
}