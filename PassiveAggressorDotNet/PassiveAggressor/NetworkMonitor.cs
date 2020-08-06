using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PcapDotNet.Core;
using PcapDotNet.Packets;


namespace PassiveAggressor
{
    class NetworkMonitor
    {
        /// <summary>
        /// Fire update event no more often than this
        /// </summary>
        public double MinUpdateIntervalSeconds { get; set; } = 0.001;

        public class Host
        {
            public DateTime LastSeen;
            public PcapDotNet.Packets.IpV4.IpV4Address? IpV4Address = null;
            public PcapDotNet.Packets.IpV6.IpV6Address? IpV6Address = null;
        }

        public delegate void HostListChanged_d(List<Host> hostList);
        public event HostListChanged_d HostListChanged;

        public Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host> HostList { get; private set; } = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host>();

        private PacketCommunicator communicator;

        public void StartListening()
        {

            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Print the list
            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                Console.Write((i + 1) + ". " + device.Name);
                if (device.Description != null)
                    Console.WriteLine(" (" + device.Description + ")");
                else
                    Console.WriteLine(" (No description available)");
            }
            int devIdx = 3;

            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[devIdx];

            // Open the device
            communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                                                                            // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000);                                  // read timeout
            Console.WriteLine("Listening on " + selectedDevice.Description + "...");
        }

        public void GetTestPacket()
        {
            Packet packet;
            PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
            //communicator.ReceiveSomePackets()
            switch (result)
            {
                case PacketCommunicatorReceiveResult.Timeout:
                    // Timeout elapsed
                    break;
                case PacketCommunicatorReceiveResult.Ok:
                    //Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" +
                    //                  packet.Length);
                    Console.WriteLine("Saw packet from MAC " + packet.Ethernet.Source);
                    break;
                default:
                    throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
            }
        }
    }
}
