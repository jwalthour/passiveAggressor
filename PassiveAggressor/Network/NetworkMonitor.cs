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
        public double UpdateIntervalSeconds { get; set; } = 0.1;

        /// <summary>
        /// Time we most recently fired HostListChanged
        /// </summary>
        private DateTime lastUpdateTime = new DateTime();

        /// <summary>
        /// A host detected by the Monitor
        /// </summary>
        public class Host
        {
            public Host(PcapDotNet.Packets.Ethernet.MacAddress mac, PcapDotNet.Packets.IpV4.IpV4Address host, DeviceAddress intf)
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


        /// <summary>
        /// Host observations that still need to be checked and incorporated into the Hosts dictionary
        /// </summary>
        private Queue<Host> hostsToIncorporate = new Queue<Host>();

        /// <summary>
        /// Hosts that are ready to deliver (that is, confirmed to be local addresses)
        /// </summary>
        private Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host> Hosts = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host>();

        /// <summary>
        /// Event fired to indicate changes to HostList
        /// </summary>
        /// <param name="hosts">The updated list of hosts</param>
        public delegate void HostListChanged_d(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Host> hosts);
        /// <summary>
        /// Event fired to indicate changes to Hosts list
        /// </summary>
        public event HostListChanged_d HostListChanged;

        /// <summary>
        /// The objects used to manage one interface
        /// </summary>
        public class Interface
        {
            private PacketCommunicator Communicator = null;
            public DeviceAddress IpV4Address { get; private set; } = null;
            private LivePacketDevice Device;
            private BackgroundWorker MonitorWorker;

            /// <summary>
            /// Reference to the shared queue being written to by all interface listeners and
            /// read by the consumer
            /// </summary>
            private Queue<Host> outputQueue;


            /// <summary>
            /// Receive the first this many bytes of each packet
            /// </summary>
            private const int PACKET_RX_LEN_B = 1024;

            /// <summary>
            /// Check this often for worker cancellation
            /// </summary>
            private const int PACKET_RX_TIMEOUT_MS = 100;

            /// <summary>
            /// Intended to perform any CPU-bound work to free up the other threads to listen for packets
            /// </summary>
            private BackgroundWorker packetProcessorWorker;

            public Interface(LivePacketDevice device, Queue<Host> outputQueue)
            {
                this.Device = device;
                this.outputQueue = outputQueue;
            }

            public bool Listening
            {
                get { return Communicator != null; }
                set
                {
                    if(value)
                    {
                        if(Listening)
                        {
                            StopListening();
                        }
                        StartListening();
                    } else
                    {
                        StopListening();
                    }
                }
            }

            /// <summary>
            /// Cease listening on this interface
            /// </summary>
            private void StopListening()
            {
                // TODO
            }

            /// <summary>
            /// Launch a background thread to listen for packets on this interface
            /// </summary>
            private void StartListening()
            {
                foreach (DeviceAddress addr in Device.Addresses)
                {
                    if (addr.Address.Family == SocketAddressFamily.Internet)
                    {
                        IpV4Address = addr;
                    }
                }

                try
                {
                    Communicator = Device.Open(PACKET_RX_LEN_B, PacketDeviceOpenAttributes.Promiscuous, PACKET_RX_TIMEOUT_MS);
                    // Filter to only IPv4 packets so we can assume they have an IPv4 header later
                    using (BerkeleyPacketFilter filter = Communicator.CreateFilter("ip"))
                    {
                        Communicator.SetFilter(filter);
                    }


                    if (Communicator.DataLink.Kind != DataLinkKind.Ethernet)
                    {
                        Console.WriteLine("This program works only on Ethernet networks; skipping interface named " + Device.Name + " (" + Device.Description + ")");
                        Communicator = null;
                    }
                    else
                    {
                        MonitorWorker = new BackgroundWorker();
                        MonitorWorker.DoWork += processPackets;
                        MonitorWorker.WorkerSupportsCancellation = true;
                        MonitorWorker.WorkerReportsProgress = false;
                        MonitorWorker.RunWorkerAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to open interface named " + Device.Name + " (" + Device.Description + "): " + ex);
                    Communicator = null;
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
                try
                {
                    while (!worker.CancellationPending)
                    {
                        Packet packet;
                        // We're only listening during the below function call.  Thus, all of this thread outside of this function call should be as fast as it can be.
                        PacketCommunicatorReceiveResult result = Communicator.ReceivePacket(out packet);
                        //communicator.ReceiveSomePackets()
                        switch (result)
                        {
                            case PacketCommunicatorReceiveResult.Timeout:
                                // Timeout elapsed
                                break;
                            case PacketCommunicatorReceiveResult.Ok:
                                // Chuck it in the queue to evaluate on another thread
                                Host host = new Host(packet.Ethernet.Source, packet.Ethernet.IpV4.Source, IpV4Address);
                                lock (outputQueue)
                                {
                                    outputQueue.Enqueue(host);
                                }
                                break;
                            default:
                                throw new InvalidOperationException("The result " + result + " should never be reached here");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Caught exception in listener thread: " + ex);
                }
                finally
                {
                    Communicator = null;
                }
            }
        }

        /// <summary>
        /// Interfaces detected on this machine
        /// Keys are device names
        /// </summary>
        public Dictionary<string, Interface> Interfaces { get; private set; } = new Dictionary<string, Interface>();
        /// <summary>
        /// Intended to perform any CPU-bound work to free up the other threads to listen for packets
        /// </summary>
        private BackgroundWorker packetProcessorWorker;
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

            foreach (LivePacketDevice device in allDevices)
            {
                Interface intf = new Interface(device, hostsToIncorporate);
                Interfaces.Add(device.Name, intf);
                intf.Listening = true;
            }

            packetProcessorWorker = new BackgroundWorker();
            packetProcessorWorker.DoWork += incorporatePackets;
            packetProcessorWorker.WorkerSupportsCancellation = true;
            packetProcessorWorker.WorkerReportsProgress = false;
            packetProcessorWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Check hosts added to hostsToIncorporate and incorporate them into the Hosts dictionary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void incorporatePackets(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            try
            {
                while (!worker.CancellationPending)
                {
                    while (hostsToIncorporate.Count > 0) // assumes Queue.Count is atomic and thus automatically thread-safe
                    {
                        Host host = null;

                        // This lock needs to be very short because the listener threads aren't listening while they wait for the lock
                        lock (hostsToIncorporate)
                        {
                            if (hostsToIncorporate.Count > 0)
                            {
                                host = hostsToIncorporate.Dequeue();
                            }
                        }

                        if (host != null)
                        {
                            // Is this outbound from the interface on which it was captured?
                            if (!host.IntfIpV4Address.Address.EqualsAddr(host.HostIpV4Address))
                            {
                                // Is this from the same subnet as the interface on which it was captured?
                                if (host.IntfIpV4Address.SubnetContains(host.HostIpV4Address))
                                {
                                    Hosts[host.HostMacAddress] = host;

                                    if (DateTime.Now > lastUpdateTime.AddSeconds(UpdateIntervalSeconds))
                                    {
                                        HostListChanged?.Invoke(Hosts);
                                        lastUpdateTime = DateTime.Now;
                                    }
                                    else
                                    {
                                        //Console.WriteLine("Too soon for update");
                                    }
                                }
                                else
                                {
                                    //Console.WriteLine("Outside of subnet: " + host.HostIpV4Address);
                                }
                            }
                        }
                        else
                        {
                            //Console.WriteLine("Same host: " + host.HostIpV4Address);
                        }
                    }
                    // An inelegant way to avoid spinlock - sleep for about 1/1000th of the update interval
                    System.Threading.Thread.Sleep((int)(UpdateIntervalSeconds) + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception in listener thread: " + ex);
            }
        }

    }
}
