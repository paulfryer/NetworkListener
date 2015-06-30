using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.Azure;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NetworkListener.Devices;
using Newtonsoft.Json;
using vtortola.WebSockets;

namespace NetworkListener.Modules
{
    public class HomeModule : CustomRequestVerbsModule
    {
        //private static readonly NamespaceManager Manager = new NamespaceManager("mynamespace .servicebus.windows.net");
       // private static readonly EventHubDescription EventHubDescription = Manager.CreateEventHubIfNotExists("MyEventHub");

        private const string hubname = "mp3processing";

        private readonly EventHubClient client =
            EventHubClient.CreateFromConnectionString(
                ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"], hubname);


        public HomeModule()
        {
        


            Get["/listen"] = p => View["Listen"];
            
            Get["/test"] = parameters =>
            {
                Console.Write("TESTING NANCY WEB SERVER.");
                return "Hello World!!";
            };
            
            OnError.AddItemToEndOfPipeline((a1, a2) =>
            {
                Console.WriteLine("ERROR!!!!!!!!!!!!!!!" + a1 + " " + a2); 
                return null;
            });


            Notify["/notify"] = _ =>
            {
                try
                {
                    Console.WriteLine("Processing NOTIFY Notification: " + Request.Body.Length);
                    if (Request.Body != null && Request.Body.Length > 0)
                    {
                        var len = Convert.ToInt32(Request.Body.Length);
                        var buffer = new byte[len];
                        Request.Body.Read(buffer, 0, len);
                        var content = Encoding.UTF8.GetString(buffer);
                        
                        SendToWebSocket(content);

                        if (Request.Headers["NT"] != null)
                            Console.WriteLine("NT: " + Request.Headers["NT"].First());
                        if (Request.Headers["NTS"] != null)
                            Console.WriteLine("NTS: " + Request.Headers["NTS"].First());
                        if (Request.Headers["SID"] != null)
                            Console.WriteLine("SID: " + Request.Headers["SID"].First());

                        var eventData = new EventData(buffer);
                        foreach (var header in Request.Headers)
                            foreach (var value in header.Value)
                            {
                                eventData.Properties.Add(header.Key, value);
                            }

                        
                        eventData.Properties.Add("UserHostAddress", Context.Request.UserHostAddress);
                        eventData.Properties.Add("ApplicationCode", "NetworkListener");

                        client.Send(eventData);

                        // TODO: consider batching..
                        Console.WriteLine("SENT EVENT TO EVENT HUB");
                    }
        
                }
                catch (Exception e)
                {
                    Console.WriteLine("===============================================");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("===============================================");
                }


                return 200;
            };


            Notify["/OLD_notify"] = _ =>
            {
                try
                {
                    
                    Console.WriteLine("Processing NOTIFY Notification: " + Request.Body.Length);
                    if (Request.Body != null && Request.Body.Length > 0)
                    {
                        var len = Convert.ToInt32(Request.Body.Length);
                        var buffer = new byte[len];
                        Request.Body.Read(buffer, 0, len);
                        var content = Encoding.UTF8.GetString(buffer);
                        
                        SendToWebSocket(content);

                        if (Request.Headers["NT"] != null)
                            Console.WriteLine("NT: " + Request.Headers["NT"].First());
                        if (Request.Headers["NTS"] != null)
                            Console.WriteLine("NTS: " + Request.Headers["NTS"].First());
                        if (Request.Headers["SID"] != null)
                            Console.WriteLine("SID: " + Request.Headers["SID"].First());
                        
                        var serviceNamespace = "musicindexer-west";
                        var hubname = "mp3processing";
                        var keyName = "EventProvider";
                        var key = "z/YufBaB+oluymSGwOL0fFpsUiphZ7dvS28HU3fFh5g=";
                        var uri = "https://" + serviceNamespace + ".servicebus.windows.net" + "/" + hubname + "/messages";

                        var expiry =
                            (Int32)
                                (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds +
                                 TimeSpan.FromHours(1).TotalSeconds);
                        var string_to_sign = WebUtility.UrlEncode(uri) + "\n" + expiry;
                        var stringToSignBytes = Encoding.UTF8.GetBytes(string_to_sign);
                        var hmac = HMAC.Create("HMACSHA256");
                        hmac.Key = Encoding.UTF8.GetBytes(key);
                        var hash = hmac.ComputeHash(stringToSignBytes);
                        var signature = Convert.ToBase64String(hash);
                        var token = "SharedAccessSignature sr=" + WebUtility.UrlEncode(uri) + "&sig=" +
                                    WebUtility.UrlEncode(signature) + "&se=" + expiry + "&skn=" + keyName;
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("Authorization", token);
                        var message = new HttpRequestMessage
                        {
                            RequestUri = new Uri(uri),
                            Method = HttpMethod.Post,
                            Content = new StringContent(content, Encoding.UTF8, "application/xml")
                        };
                        var resp = httpClient.SendAsync(message).Result;
                        Console.WriteLine("RESPONSE FROM EVENT HUB: " + resp.ReasonPhrase);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("===============================================");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("===============================================");
                }
                return 200;
            };
        }

        private async void SendToWebSocket(string data)
        {
            try
            {


                if (data.Contains("<"))
                {
                    var eventXml = XElement.Parse(data);
                    var lastChangeNode =
                        eventXml.Descendants(XName.Get("LastChange"))
                            .SingleOrDefault();
                    if (lastChangeNode != null)
                    {
                        var rawEvent = XElement.Parse(lastChangeNode.Value);
                        var currentTrackMetaDataNode = rawEvent
                            .Descendants(XName.Get("CurrentTrackMetaData",
                                "urn:schemas-upnp-org:metadata-1-0/AVT/"))
                            .SingleOrDefault(n => !String.IsNullOrEmpty(n.Attribute("val").Value));
                        if (currentTrackMetaDataNode != null)
                        {
                            var metadata = currentTrackMetaDataNode.Attribute(XName.Get("val")).Value;
                            var metaXml = XElement.Parse(metadata);
                            var classTypeNode =
                                metaXml.Descendants(XName.Get("class", "urn:schemas-upnp-org:metadata-1-0/upnp/"))
                                    .SingleOrDefault();
                            if (classTypeNode != null)
                            {
                                var classType = classTypeNode.Value;
                                switch (classType)
                                {
                                    case "object.item.audioItem.musicTrack":
                                        var res =
                                            metaXml.Descendants(XName.Get("res",
                                                "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"))
                                                .SingleOrDefault();
                                        if (res != null)
                                        {
                                            var urlString = res.Value;
                                            var albumNode =
                                                metaXml.Descendants(XName.Get("album",
                                                    "urn:schemas-upnp-org:metadata-1-0/upnp/"))
                                                    .SingleOrDefault();
                                            var album = albumNode == null ? "" : albumNode.Value;
                                            var track =
                                                metaXml.Descendants(XName.Get("title",
                                                    "http://purl.org/dc/elements/1.1/"))
                                                    .Single()
                                                    .Value;

                                            var artistNode =
                                                metaXml.Descendants(XName.Get("creator",
                                                    "http://purl.org/dc/elements/1.1/"))
                                                    .SingleOrDefault();

                                            var artist = artistNode == null ? "" : artistNode.Value;

                                            urlString = urlString.Replace("pndrradio-", string.Empty);
                                            var fileLocation = new Uri(urlString);
                                            var albumArtUriNode = metaXml.Descendants(XName.Get("albumArtURI",
                                                "urn:schemas-upnp-org:metadata-1-0/upnp/")).SingleOrDefault();
                                            Uri albumArtUri = null;
                                            if (albumArtUriNode != null)
                                                if (!string.IsNullOrEmpty(albumArtUriNode.Value) &&
                                                    albumArtUriNode.Value.StartsWith("http"))
                                                    albumArtUri = new Uri(albumArtUriNode.Value);

                                            var start = DateTime.UtcNow;
                                            var position = await SonosDevice.GetPositionInfo(Request.UserHostAddress);
                                            var durration = DateTime.UtcNow.Subtract(start);
                                            position = (int) (position + durration.TotalSeconds);


                                            var t = new Track
                                            {
                                                album = album,
                                                albumArtUri = albumArtUri,
                                                artist = artist,
                                                fileLocation = fileLocation,
                                                track = track,
                                                position = position
                                            };

                                            var content = JsonConvert.SerializeObject(t);


                                            foreach (var ws in Program.WebSockets)
                                                if (ws.IsConnected)
                                                    ws.WriteString(content);


                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("COULD NOT SEND MESSAGE TO WEBSOCKET: " + ex);
            }


        }
    }

    public class Track
    {
        public string artist { get; set; }
        public string album { get; set; } 
        public string track { get; set; }
        public Uri fileLocation { get; set; }
        public Uri albumArtUri { get; set; }
        public int position { get; set; }
    }
}