using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetworkListener.Devices
{
    public class SonosDevice : Device
    {
        public string MinCompatibleVersion { get; set; }
        public string LegacyCompatibleVersion { get; set; }
        public string DisplayVersion { get; set; }
        public string ExtraVersion { get; set; }
        public string RoomName { get; set; }
        public string DisplayName { get; set; }
        public int ZoneType { get; set; }
        public string Feature1 { get; set; }
        public string Feature2 { get; set; }
        public string Feature3 { get; set; }
        public int InternalSpeakerSize { get; set; }
        public decimal BassExtension { get; set; }
        public decimal SatGainOffset { get; set; }
        public int Memory { get; set; }
        public int Flash { get; set; }
        public int AmpOnTime { get; set; }

        /*
		<minCompatibleVersion>27.0-00000</minCompatibleVersion>
		<legacyCompatibleVersion>24.0-0000</legacyCompatibleVersion>
		<displayVersion>5.2</displayVersion>
		<extraVersion>OTP:</extraVersion>
		<roomName>Media Room</roomName>
		<displayName>PLAY:1</displayName>
		<zoneType>9</zoneType>
		<feature1>0x00000000</feature1>
		<feature2>0x00403336</feature2>
		<feature3>0x0001000e</feature3>
		<internalSpeakerSize>5</internalSpeakerSize>
		<bassExtension>75.000</bassExtension>
		<satGainOffset>6.000</satGainOffset>
		<memory>128</memory>
		<flash>64</flash>
		<ampOnTime>10</ampOnTime>
*/

        public static async Task<int> GetPositionInfo(string ipAddress)
        {
            var xml =
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:GetPositionInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID></u:GetPositionInfo></s:Body></s:Envelope>";
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                MakeUri(ipAddress, "/MediaRenderer/AVTransport/Control"))
            {
                Content = new StringContent(xml)
            };
            httpClient.DefaultRequestHeaders.Add("USER-AGENT",
                "Linux UPnP/1.0 Sonos/28.1-86200 (WDCR:Microsoft Windows NT 6.2.9200.0)");
            request.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo\"");
            var response = await httpClient.SendAsync(request);

            var bytes = await response.Content.ReadAsByteArrayAsync();

            var respXml = Encoding.UTF8.GetString(bytes);
            var element = XElement.Parse(respXml);

            var timeString = element.Descendants("RelTime").Single().Value;

            var seconds = TimeSpan.Parse(timeString).TotalSeconds;

            return Convert.ToInt16(seconds);
        }

        private static Uri MakeUri(string ipAddress, string relativeUrl)
        {
            return new Uri("http://" + ipAddress + ":1400" + relativeUrl);
        }
    }
}