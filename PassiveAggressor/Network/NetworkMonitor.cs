using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PcapDotNet.Core;


namespace PassiveAggressor.Network
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
        /// Host observations that still need to be checked and incorporated into the Hosts dictionary
        /// </summary>
        private Queue<ObservedHost> hostsToIncorporate = new Queue<ObservedHost>();

        /// <summary>
        /// Hosts that are ready to deliver (that is, confirmed to be local addresses)
        /// Dictionary goes:
        /// manufacturer string -> Host -> detailed host info
        /// </summary>
        //private Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost> Hosts = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost>();
        private Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost>> Hosts = new Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost>>();

        /// <summary>
        /// Event fired to indicate changes to HostList
        /// </summary>
        /// <param name="hosts">The updated list of hosts</param>
        public delegate void HostListChanged_d(Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost>> hosts);
        /// <summary>
        /// Event fired to indicate changes to Hosts list
        /// </summary>
        public event HostListChanged_d HostListChanged;

        /// <summary>
        /// Interfaces detected on this machine
        /// Keys are device names
        /// </summary>
        public Dictionary<string, ListeningInterface> Interfaces { get; private set; } = new Dictionary<string, ListeningInterface>();
        /// <summary>
        /// Intended to perform any CPU-bound work to free up the other threads to listen for packets
        /// </summary>
        private BackgroundWorker packetProcessorWorker;

        private ManufacturerData mfrData = new ManufacturerData();

        /// <summary>
        /// Open interfaces for listening, load manufacturer lookup file, load nickname data
        /// </summary>
        public void Initialize()
        {
            mfrData.LoadMfrData();
            InitializeInterfaces();
        }

        /// <summary>
        /// Find interfaces and open them all for listening
        /// </summary>
        private void InitializeInterfaces()
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
                ListeningInterface intf = new ListeningInterface(device, hostsToIncorporate);
                Interfaces.Add(device.Name, intf);
                // Auto start listening so any errors will appear early
                intf.StartListening();
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
            ObservedHost host = null;

            try
            {
                while (!worker.CancellationPending)
                {
                    while (hostsToIncorporate.Count > 0) // assumes Queue.Count is atomic and thus automatically thread-safe
                    {
                        host = null;

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
                            if (host.IntfIpV4Address == null || !host.IntfIpV4Address.Address.EqualsAddr(host.HostIpV4Address))
                            {
                                // Is this from the same subnet as the interface on which it was captured?
                                if (host.IntfIpV4Address == null || host.IntfIpV4Address.SubnetContains(host.HostIpV4Address))
                                {
                                    //TODO: if host.IntfIpV4Address == null, check if it's an internal address.  Discard WAN addresses.

                                    // Store in appropriate dictionaries
                                    host.ManufacturerDescription = mfrData.GetMfrNameForMac(host.HostMacAddress);

                                    if(!Hosts.ContainsKey(host.ManufacturerDescription))
                                    {
                                        Hosts.Add(host.ManufacturerDescription, new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost>());
                                    }
                                    Hosts[host.ManufacturerDescription][host.HostMacAddress] = host;

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

        /// <summary>
        /// Empty out the hosts list
        /// </summary>
        public void ClearHostsList()
        {
            Hosts.Clear();
            HostListChanged?.Invoke(Hosts);
            lastUpdateTime = DateTime.Now;
        }

        #region Passthroughs
        /// <summary>
        /// Return the resource name indicating a PNG file containing the icon for this manufacturer name.
        /// Will return a sensible default icon if an icon is not available.
        /// </summary>
        /// <param name="mfr"></param>
        /// <returns></returns>
        public string GetIconResourceNameForMfr(string mfr)
        {
            return mfrData.GetIconResourceNameForMfr(mfr);
        }

        #endregion Passthroughs
    }
}
