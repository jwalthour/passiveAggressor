using System;

using PcapDotNet.Core;


namespace PassiveAggressor.Network
{
    /// <summary>
    /// A host detected by the Monitor.
    /// This is a struct-like class used to pass data from the main logic layer to the GUI layer.
    /// </summary>
    public class ObservedHost
    {
        public ObservedHost(PcapDotNet.Packets.Ethernet.MacAddress mac, PcapDotNet.Packets.IpV4.IpV4Address host, DeviceAddress intf)
        {
            ManufacturerDescription = "Unknown Manufacturer";
            LastSeen = DateTime.Now;
            HostMacAddress = mac;
            HostIpV4Address = host;
            IntfIpV4Address = intf;
        }
        /// <summary>
        /// Human-readable name of the interface manufacturer that ManufacturerData has reported for this HostMacAddress
        /// </summary>
        public string ManufacturerDescription;
        public DateTime LastSeen;
        public PcapDotNet.Packets.Ethernet.MacAddress HostMacAddress;
        public PcapDotNet.Packets.IpV4.IpV4Address HostIpV4Address;
        public DeviceAddress IntfIpV4Address;
        // TODO: IPv6 support
        //public PcapDotNet.Packets.IpV6.IpV6Address? IpV6Address = null;

        public override int GetHashCode()
        {
            return HostMacAddress.GetHashCode();
        }
    }
}
