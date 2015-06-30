using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetworkListener.Devices
{
    public static class DeviceParser
    {
        public static async Task<Device> Load(Uri deviceDescriptionUrl)
        {
            var httpClient = new HttpClient();
            var message = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = deviceDescriptionUrl
            };

            var response = await httpClient.SendAsync(message);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var bytes = response.Content.ReadAsByteArrayAsync().Result;
                var xml = Encoding.UTF8.GetString(bytes);
                try
                {
                    var d = XElement.Parse(xml);
                    return Parse(d, deviceDescriptionUrl);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while trying to pase XML: " + e);
                }
            }
            return null;
        }


        private static string TrySetString(XElement d, string property)
        {
            var deviceTypeNode = d.Descendants(XName.Get(property, "urn:schemas-upnp-org:device-1-0")).FirstOrDefault();
            if (deviceTypeNode != null)
            {
                return deviceTypeNode.Value;
            }
            return null;
        }

        private static Uri TrySetUri(XElement d, Uri baseUri, string elementName)
        {
            var controlUrlString = TrySetString(d, elementName);

            //if (!controlUrlString.StartsWith("/"))
            //    controlUrlString = "/" + controlUrlString;
            //if (baseUri.AbsoluteUri.EndsWith("/"))
            //    baseUri = new Uri(baseUri.AbsoluteUri.Substring(0, baseUri.AbsoluteUri.Length - 1));

            var serviceControlUrl = new Uri(baseUri, controlUrlString);
            //Uri.TryCreate(baseUri, new Uri(controlUrlString), out serviceControlUrl);


            return serviceControlUrl;
        }

        public static Device Parse(XElement d, Uri deviceDescriptionUrl)
        {
            var baseUri =
                new Uri(deviceDescriptionUrl.Scheme + Uri.SchemeDelimiter + deviceDescriptionUrl.Host + ":" +
                        deviceDescriptionUrl.Port);
            var device = new Device
            {
                Manufacturer = new Manufacturer()
            };
            //var d = XElement.Parse(xml);
            device.DeviceType = TrySetString(d, "deviceType");
            device.FriendlyName = TrySetString(d, "friendlyName");
            device.Manufacturer.Name = TrySetString(d, "manufacturer");
            var manufacturerUrlString = TrySetString(d, "manufacturerURL");
            if (!string.IsNullOrEmpty(manufacturerUrlString))
                device.Manufacturer.Url = new Uri(manufacturerUrlString);
            var serviceListNode =
                d.Descendants(XName.Get("serviceList", "urn:schemas-upnp-org:device-1-0")).FirstOrDefault();


            if (serviceListNode != null)
            {
                device.Services = new List<Service>();
                foreach (
                    var serviceNode in
                        serviceListNode.Descendants(XName.Get("service", "urn:schemas-upnp-org:device-1-0")))
                {
                    var service = new Service();
                    service.ServiceType = TrySetString(serviceNode, "serviceType");
                    service.ServiceId = TrySetString(serviceNode, "serviceId");
                    service.ControlUrl = TrySetUri(serviceNode, baseUri, "controlURL");
                    service.EventSubscriptionUrl = TrySetUri(serviceNode, baseUri, "eventSubURL");
                    service.ServiceControlProtocolDocumentUrl = TrySetUri(serviceNode, baseUri, "SCPDURL");

                    // Note: we need to move this into a method so it is optional. Performance hit to download every time.
                    /*
                    var serviceDoc = XElement.Load(service.ServiceControlProtocolDocumentUrl.AbsoluteUri);
                    var serviceStateTableNode =
                        serviceDoc.Descendants(XName.Get("serviceStateTable", "urn:schemas-upnp-org:service-1-0"))
                            .FirstOrDefault();
                    if (serviceStateTableNode != null)
                    {
                        service.StateVariables = new List<StateVarabile>();
                        foreach (
                            var v in
                                serviceStateTableNode.Descendants(XName.Get("stateVariable",
                                    "urn:schemas-upnp-org:service-1-0")))
                        {
                            var stateVar = new StateVarabile();
                            stateVar.Name =
                                v.Descendants(XName.Get("name", "urn:schemas-upnp-org:service-1-0")).First().Value;
                            stateVar.DataType =
                                v.Descendants(XName.Get("dataType", "urn:schemas-upnp-org:service-1-0")).First().Value;
                            var attribute = v.Attribute("sendEvents");
                            if (attribute != null)
                            {
                                var sendEventsString = attribute.Value.ToLower();
                                if (sendEventsString == "yes" || sendEventsString == "1" || sendEventsString == "true")
                                    stateVar.SendEvents = true;
                                else stateVar.SendEvents = false;
                            }
                            service.StateVariables.Add(stateVar);
                        }
                    }*/

                    device.Services.Add(service);
                }
            }
            var deviceListNode =
                d.Descendants(XName.Get("deviceList", "urn:schemas-upnp-org:device-1-0")).FirstOrDefault();
            if (deviceListNode != null)
            {
                device.Devices = new List<Device>();
                foreach (
                    var deviceNode in
                        deviceListNode.Descendants(XName.Get("device", "urn:schemas-upnp-org:device-1-0")))
                {
                    Console.WriteLine("ABOUT TO PROCESS SUB DEVICE with namespace: " +
                                      deviceNode.GetDefaultNamespace().NamespaceName);
                    var subDevice = Parse(deviceNode, deviceDescriptionUrl);
                    device.Devices.Add(subDevice);
                }
            }

            return device;
        }
    }
}