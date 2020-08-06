using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassiveAggressor
{
    public static class AddressExtensions
    {
        /// <summary>
        /// Check a SocketAddress to see if it matches an IpV4Address.
        /// Needs to have a different name other than Equals() because there's already an Equals(Object) defined.
        /// </summary>
        /// <param name="self">The `this` object</param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool EqualsAddr(this PcapDotNet.Core.SocketAddress self, PcapDotNet.Packets.IpV4.IpV4Address other)
        {
            if(self.Family != PcapDotNet.Core.SocketAddressFamily.Internet)
            {
                // Not IPv4
                return false;
            } else
            {
                return other.Equals((self as PcapDotNet.Core.IpV4SocketAddress).Address);
                //return other.ToValue() == (self as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue();
            }
        }

        /// <summary>
        /// Check if an address belongs to the same subnet as this DeviceAddress, based on its subnet mask
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SubnetContains(this PcapDotNet.Core.DeviceAddress self, PcapDotNet.Packets.IpV4.IpV4Address other)
        {
            if(self.Address.Family != PcapDotNet.Core.SocketAddressFamily.Internet)
            {
                // Not IPv4
                return false;
            } else
            {
                // Perform mask
                uint maskedSubnet = (self.Address as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue() & (self.Netmask as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue();
                uint maskedOther = other.ToValue() & (self.Netmask as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue();

                return (maskedOther == maskedSubnet);
            }
        }
    }
}
