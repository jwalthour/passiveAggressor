using System;

using PcapDotNet.Core;


namespace PassiveAggressor
{
    /// <summary>
    /// A host detected by the Monitor
    /// </summary>
    public class ObservedHost
    {
        public ObservedHost(PcapDotNet.Packets.Ethernet.MacAddress mac, PcapDotNet.Packets.IpV4.IpV4Address host, DeviceAddress intf)
        {
            LastSeen = DateTime.Now;
            HostMacAddress = mac;
            HostIpV4Address = host;
            IntfIpV4Address = intf;
        }
        public DateTime LastSeen;
        public PcapDotNet.Packets.Ethernet.MacAddress HostMacAddress;
        public PcapDotNet.Packets.IpV4.IpV4Address HostIpV4Address;
        public DeviceAddress IntfIpV4Address;
        // TODO: IPv6 support
        //public PcapDotNet.Packets.IpV6.IpV6Address? IpV6Address = null;
    }
}
