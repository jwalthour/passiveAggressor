﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Net.NetworkInformation;

namespace PassiveAggressor.Network
{
    /// <summary>
    /// The objects used to manage one interface
    /// </summary>
    public class ListeningInterface
    {
        private PacketCommunicator communicator = null;
        public DeviceAddress IpV4Address { get; private set; } = null;
        public string Description { get { return device.Description; } }
        private LivePacketDevice device;
        private BackgroundWorker monitorWorker;

        /// <summary>
        /// If an error was encountered, this will be populated with a human-readable message
        /// </summary>
        public string ErrorMessage { get; private set; } = "";

        /// <summary>
        /// Reference to the shared queue being written to by all interface listeners and
        /// read by the consumer
        /// </summary>
        private Queue<ObservedHost> outputQueue;

        /// <summary>
        /// Receive the first this many bytes of each packet
        /// </summary>
        private const int PACKET_RX_LEN_B = 1024;

        /// <summary>
        /// Check this often for worker cancellation
        /// </summary>
        private const int PACKET_RX_TIMEOUT_MS = 100;
        
        public ListeningInterface(LivePacketDevice device, Queue<ObservedHost> outputQueue)
        {
            this.device = device;
            this.outputQueue = outputQueue;
            foreach (DeviceAddress addr in this.device.Addresses)
            {
                if (addr.Address.Family == SocketAddressFamily.Internet)
                {
                    IpV4Address = addr;
                }
            }
        }

        /// <summary>
        /// true indicates that this interface is up and reporting host observations.
        /// </summary>
        public bool Listening
        {
            get { return communicator != null; }
        }

        public delegate void ListeningChanged_d(bool isListeningNow);
        /// <summary>
        /// Fired to indicate when this interface has begin or stopped listening
        /// </summary>
        public event ListeningChanged_d ListeningChanged;

        /// <summary>
        /// Cease listening on this interface
        /// </summary>
        public void StopListening()
        {
            monitorWorker?.CancelAsync();
            ListeningChanged?.Invoke(false);
        }

        /// <summary>
        /// Launch a background thread to listen for packets on this interface
        /// </summary>
        public void StartListening()
        {
            ErrorMessage = "";

            try
            {
                communicator = device.Open(PACKET_RX_LEN_B, PacketDeviceOpenAttributes.Promiscuous, PACKET_RX_TIMEOUT_MS);
                // Filter to only IPv4 packets so we can assume they have an IPv4 header later
                using (BerkeleyPacketFilter filter = communicator.CreateFilter("ip"))
                {
                    communicator.SetFilter(filter);
                }

                if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                {
                    ErrorMessage = "Not an Ethernet interface.";
                    Console.WriteLine("This program works only on Ethernet networks; skipping interface named " + device.Name + " (" + device.Description + ")");
                    StoppedListening();
                }
                else
                {
                    monitorWorker = new BackgroundWorker();
                    monitorWorker.DoWork += listen;
                    monitorWorker.WorkerSupportsCancellation = true;
                    monitorWorker.WorkerReportsProgress = false;
                    monitorWorker.RunWorkerAsync();
                    ListeningChanged?.Invoke(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open interface named " + device.Name + " (" + device.Description + "): " + ex);
                ErrorMessage = "Failed to open interface: " + ex.GetType() + ".";
                StoppedListening(); 
            }
        }

        /// <summary>
        /// Loop through packets until cancelled
        /// </summary>
        /// <param name="sender">Assumed to be the parent BackgroundWorker object</param>
        /// <param name="e"></param>
        private void listen(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ErrorMessage = "";
            try
            {
                while (!worker.CancellationPending)
                {
                    communicator.ReceivePackets(5, p => processPacket(p));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception in listener thread: " + ex);
                ErrorMessage = "Failed to listen: " + ex.GetType() + ".";
            }
            finally
            {
                StoppedListening();
            }
        }
        /// <summary>
        /// Callback that incorporates one packet
        /// </summary>
        /// <param name="packet"></param>
        private void processPacket(Packet packet)
        {
            ObservedHost host = new ObservedHost(packet.Ethernet.Source, packet.Ethernet.IpV4.Source, IpV4Address);
            lock (outputQueue)
            {
                outputQueue.Enqueue(host);
            }
        }

        /// <summary>
        /// Cleanup when we stop listening
        /// </summary>
        private void StoppedListening()
        {
            communicator = null;
            ListeningChanged?.Invoke(false);
        }

        private BackgroundWorker pingSubnetWorker = null;
        public bool PingSubnetInProgress {  get { return pingSubnetWorker != null; } }

        /// <summary>
        /// Start pinging every valid IP address in this interface's subnet.
        /// Starts a background thread.
        /// </summary>
        public void BeginPingingSubnet(RunWorkerCompletedEventHandler completionHandler = null, ProgressChangedEventHandler progressHandler = null)
        {
            CancelPingingSubnet();
            pingSubnetWorker = new BackgroundWorker();
            pingSubnetWorker.DoWork += pingSubnetDoWork;
            pingSubnetWorker.WorkerSupportsCancellation = true;
            pingSubnetWorker.WorkerReportsProgress = true;
            if (progressHandler != null)
            {
                pingSubnetWorker.ProgressChanged += progressHandler;
            }
            if(completionHandler != null)
            {
                pingSubnetWorker.RunWorkerCompleted += completionHandler;
            }
            pingSubnetWorker.RunWorkerAsync();
        }
        

        /// <summary>
        /// Cancel subnet ping, if in progress
        /// </summary>
        public void CancelPingingSubnet()
        {
            if(PingSubnetInProgress)
            {
                pingSubnetWorker.CancelAsync();
            }
        }

        /// <summary>
        /// Send one ping to every host on the subnet.
        /// Should be started
        /// </summary>
        /// <param name="sender">Assumed to be the parent BackgroundWorker object</param>
        /// <param name="e"></param>
        private void pingSubnetDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            //PcapDotNet.Packets.IpV4.IpV4Address testMask = new PcapDotNet.Packets.IpV4.IpV4Address("255.0.0.0");

            //uint startAddressValue = (IpV4Address.Address as IpV4SocketAddress).Address.ToValue() & testMask.ToValue();
            //uint endAddressValue = startAddressValue + ~testMask.ToValue();
            uint startAddressValue = (IpV4Address.Address as IpV4SocketAddress).Address.ToValue() & (IpV4Address.Netmask as IpV4SocketAddress).Address.ToValue();
            uint endAddressValue = startAddressValue + ~(IpV4Address.Netmask as IpV4SocketAddress).Address.ToValue();

            uint numAddrs = endAddressValue - startAddressValue;

            PcapDotNet.Packets.IpV4.IpV4Address addr;
            string addrString = "";
            for (uint addrValue = startAddressValue; addrValue <= endAddressValue && !worker.CancellationPending; addrValue++)
            {
                using (Ping ping = new Ping()) // Cuts down memory usage when pinging large subnets
                {
                    addr = new PcapDotNet.Packets.IpV4.IpV4Address(addrValue); // Would prefer to use using() {} but it's not supported, neither is there like a "fromValue" method
                    addrString = addr.ToString();
                    //Console.WriteLine("Pinging " + addrString);
                    ping.SendAsync(addrString, 1, null);
                    worker.ReportProgress((int)(100.0 * ((addrValue - startAddressValue) / (double)numAddrs)));
                    //ping.SendAsyncCancel(); // takes a long time
                }
            }
            // Self cleanup
            pingSubnetWorker = null;
        }
    }
}
