using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        /// <summary>
        /// A host detected by the Monitor
        /// </summary>
        public class Host
        {
            public DateTime LastSeen;
            public PcapDotNet.Packets.IpV4.IpV4Address? IpV4Address = null;
            public PcapDotNet.Packets.IpV6.IpV6Address? IpV6Address = null;
        }

        public Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host> Hosts { get; private set; } = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host>();

        /// <summary>
        /// Event fired to indicate changes to HostList
        /// </summary>
        /// <param name="hosts">The updated list of hosts</param>
        public delegate void HostListChanged_d(List<Host> hosts);
        /// <summary>
        /// Event fired to indicate changes to Hosts list
        /// </summary>
        public event HostListChanged_d HostListChanged;

        /// <summary>
        /// The objects used to manage one interface
        /// </summary>
        public class Interface
        {
            public PacketCommunicator Communicator;
            public LivePacketDevice Device;
            public BackgroundWorker MonitorWorker;
        }

        /// <summary>
        /// Interfaces detected on this machine
        /// Keys are device names
        /// </summary>
        public Dictionary<string, Interface> Interfaces { get; private set; } = new Dictionary<string, Interface>();

        /// <summary>
        /// Receive the first this many bytes of each packet
        /// </summary>
        private const int PACKET_RX_LEN_B = 1024;

        /// <summary>
        /// Check this often for worker cancellation
        /// </summary>
        private const int PACKET_RX_TIMEOUT_MS = 100;

        /// <summary>
        /// Find interfaces and open them all for listening
        /// </summary>
        public void InitializeInterfaces()
        {
            // TODO: loop through any existing interfaces and stop them
            Interfaces.Clear();

            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            foreach(LivePacketDevice device in allDevices)
            {
                Interface intf = new Interface();
                intf.Communicator = device.Open(PACKET_RX_LEN_B, PacketDeviceOpenAttributes.Promiscuous, PACKET_RX_TIMEOUT_MS);
                intf.Device = device;
                intf.MonitorWorker = new BackgroundWorker();
                intf.MonitorWorker.DoWork += processPackets;
                intf.MonitorWorker.WorkerSupportsCancellation = true;
                intf.MonitorWorker.WorkerReportsProgress = false;
                intf.MonitorWorker.RunWorkerAsync(intf);
                Interfaces.Add(device.Name, intf);
            }
        }

        /// <summary>
        /// Loop through packets until cancelled
        /// </summary>
        /// <param name="sender">Assumed to be the parent BackgroundWorker object</param>
        /// <param name="e">Assumed to be the NetworkMonitor.Interface object being listened to</param>
        private void processPackets(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Interface intf = e.Argument as Interface;

            while (!worker.CancellationPending)
            {

                Packet packet;
                PacketCommunicatorReceiveResult result = intf.Communicator.ReceivePacket(out packet);
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
                        throw new InvalidOperationException("The result " + result + " should never be reached here");
                }
            }
        }
    }
}
