using System;

namespace NetworkListener
{
    public class SimpleSearchResponseParser : ISearchResponseParser
    {
        public ServiceDescription Parse(string searchResponseString)
        {
            var searchResponse = new ServiceDescription();

            var lines = searchResponseString.Split('\n');

            foreach (var line in lines)
            {
                var cacheControl = GetValue("CACHE-CONTROL", line);
                if (cacheControl != null)
                    searchResponse.CacheControl = cacheControl;
                var location = GetValue("LOCATION", line);
                if (location != null)
                {
                    if (location == "127.0.0.1")
                        location = "http://127.0.0.1";

                    Uri locationUri;
                    Uri.TryCreate(location, UriKind.RelativeOrAbsolute, out locationUri);


                    searchResponse.Location = locationUri;
                }

                var server = GetValue("SERVER", line);
                if (server != null)
                    searchResponse.Server = server;
                var st = GetValue("ST", line);
                if (st != null)
                    searchResponse.SearchTarget = st;
                var usn = GetValue("USN", line);
                if (usn != null)
                    searchResponse.UniqueServiceName = usn;
            }

            return searchResponse;
        }

        private string GetValue(string header, string line)
        {
            string value = null;

            if (line.Length > header.Length && line.Substring(0, header.Length).ToUpper() == header.ToUpper())
            {
                value = line.Substring(header.Length,
                    line.Length - header.Length);

                if (value.Substring(0, 1) == ":")
                    value = value.Substring(1, value.Length - 1);
                if (value.Substring(0, 1) == " ")
                    value = value.Substring(1, value.Length - 1);
                if (value.Substring(value.Length - 1, 1) == "\r")
                    value = value.Substring(0, value.Length - 1);
            }


            return value;
        }
    }
}