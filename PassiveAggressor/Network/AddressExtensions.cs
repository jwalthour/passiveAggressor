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
            if (self.Family != PcapDotNet.Core.SocketAddressFamily.Internet)
            {
                // Not IPv4
                return false;
            }
            else
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
            if (self.Address.Family != PcapDotNet.Core.SocketAddressFamily.Internet)
            {
                // Not IPv4
                return false;
            }
            else
            {
                // Perform mask
                uint maskedSubnet = (self.Address as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue() & (self.Netmask as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue();
                uint maskedOther = other.ToValue() & (self.Netmask as PcapDotNet.Core.IpV4SocketAddress).Address.ToValue();

                return (maskedOther == maskedSubnet);
            }
        }

        /// <summary>
        /// Helper for sorting MAC addresses 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 to indicate a should go first, 0 to indicate sameness, 1 to indicate b should go first</returns>
        public static int CompareTo(this PcapDotNet.Base.UInt48 a, PcapDotNet.Base.UInt48 b)
        {
            if (a == b)
            {
                return 0;
            }
            else if (a < b)
            {
                return -1;
            }
            else if (a > b)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Helper for sorting MAC addresses 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 to indicate a should go first, 0 to indicate sameness, 1 to indicate b should go first</returns>
        public static int CompareTo(this PcapDotNet.Packets.Ethernet.MacAddress a, PcapDotNet.Packets.Ethernet.MacAddress b)
        {
            return a.ToValue().CompareTo(b.ToValue());
        }

        /// <summary>
        /// Get the number of leading 1s in the binary representation of addr.
        /// For example, if provided 255.255.0.0, will return 16.
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>the number of leading 1s in the binary representation of addr</returns>
        public static int GetNetmaskBitCount( this PcapDotNet.Packets.IpV4.IpV4Address addr)
        {
            uint inverseValue = ~addr.ToValue();

            // There is a better way to do this.  It uses Log2.
            // For the life of me, I can't get the Log2 function
            // to be referenced correctly.  If you figure out how to
            // get this to work, do that instead:
            //System.Numerics.BitOperations.Log2();
            // But realistically, this function normally is called less than 10 times
            // and the loop iterates a max of 32 iterations.
            // So it's no sweat to do it the stupid way.
            int i;
            for (i = 0; i < 32; i++)
            {
                uint mask = (1u << (31 - i));
                if((inverseValue & mask) > 0)
                {
                    break;
                }
            }
            return i;
        }
    }
}
