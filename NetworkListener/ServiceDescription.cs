using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NetworkListener
{
    public class ServiceDescription
    {
        public string SearchTarget { get; set; }
        public string UniqueServiceName { get; set; }
        public Uri Location { get; set; }
        public string CacheControl { get; set; }
        public string Server { get; set; }

        public void Subscribe(Uri subscriptionUri, Uri callbackUri, TimeSpan subscriptionTimeSpan)
        {
            var httpClient = new HttpClient();
            var req = new HttpRequestMessage
            {
                RequestUri = subscriptionUri,
                Version = HttpVersion.Version11
            };
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("SimpleSonos", "1.0"));
            req.Method = new HttpMethod("SUBSCRIBE");
            req.Headers.Add("CALLBACK", "<" + callbackUri.AbsoluteUri + ">");
            req.Headers.Add("NT", "upnp:event");
            req.Headers.Add("TIMEOUT", "Second-" + (int) subscriptionTimeSpan.TotalSeconds);
            req.Headers.ConnectionClose = true;
            var cch = new CacheControlHeaderValue {NoCache = true};
            req.Headers.CacheControl = cch;
            try
            {
                var resp = httpClient.SendAsync(req).Result;
                Console.WriteLine("Gor response from " + subscriptionUri.Host + ". StatusCode: " + resp.StatusCode);
                if (resp.Headers.Contains("SID"))
                    Console.WriteLine("SID: " + resp.Headers.GetValues("SID").First());
                if (resp.Headers.Contains("TIMEOUT"))
                    Console.WriteLine("TIMEOUT: " + resp.Headers.GetValues("TIMEOUT").First());
                if (resp.Headers.Contains("Server"))
                    Console.WriteLine("Server: " + resp.Headers.GetValues("Server").First());
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR TRYING TO SUBSCRIBE " + e);
            }
        }
    }
}