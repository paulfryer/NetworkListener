using System;
using System.Collections.Generic;

namespace NetworkListener.Devices
{
    public class Manufacturer
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
    }

    public class Model
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Uri Url { get; set; }
        public string Number { get; set; }
    }

    //http://upnp.org/specs/basic/UPnP-basic-Basic-v1-Device.pdf
    public class Device
    {
        public string DeviceType { get; set; }

        public string FriendlyName { get; set; }

        public Manufacturer Manufacturer { get; set; }

        public Model Model { get; set; }

        public string SoftwareVersion { get; set; }
        public string HardwareVersion { get; set; }

        public string SerialNumber { get; set; }
        public string UniversalDeviceNumber { get; set; }
        public string UniversalProductCode { get; set; }

        public List<Icon> Icons { get; set; }

        public Uri PresentationUrl { get; set; }

        public List<Service> Services { get; set; }

        public List<Device> Devices { get; set; }
    }

    public class Icon
    {
        public string Id { get; set; }
        public string Mimetype { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public string Url { get; set; }
    }

    public class Service
    {
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public Uri ServiceControlProtocolDocumentUrl { get; set; }
        public Uri ControlUrl { get; set; }
        public Uri EventSubscriptionUrl { get; set; }

        public List<Action> Actions { get; set; }
        public List<StateVarabile> StateVariables { get; set; }

        /*
		 * <serviceType>
urn:schemas-wifialliance-org:service:WFAWLANConfig:1
</serviceType>
<serviceId>urn:wifialliance-org:serviceId:WFAWLANConfig1</serviceId>
<SCPDURL>wps_scpd.xml</SCPDURL>
<controlURL>wps_control</controlURL>
<eventSubURL>wps_event</eventSubURL>
*/
    }

    public class StateVarabile
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool SendEvents { get; set; }
    }

    public class Action
    {
        public string Name { get; set; }
        public List<Argument> Arguments { get; set; }
    }

    public class Argument
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string RelatedStateVariable { get; set; }
    }
}