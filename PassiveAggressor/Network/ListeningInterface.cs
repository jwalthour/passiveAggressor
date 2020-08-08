using System;
using System.Collections.Generic;
using System.ComponentModel;

using PcapDotNet.Core;
using PcapDotNet.Packets;


namespace PassiveAggressor
{
    /// <summary>
    /// The objects used to manage one interface
    /// </summary>
    public class ListeningInterface
    {
        private PacketCommunicator Communicator = null;
        public DeviceAddress IpV4Address { get; private set; } = null;
        public string Description { get { return Device.Description; } }
        private LivePacketDevice Device;
        private BackgroundWorker MonitorWorker;

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

        /// <summary>
        /// Intended to perform any CPU-bound work to free up the other threads to listen for packets
        /// </summary>
        private BackgroundWorker packetProcessorWorker;

        public ListeningInterface(LivePacketDevice device, Queue<ObservedHost> outputQueue)
        {
            this.Device = device;
            this.outputQueue = outputQueue;
            foreach (DeviceAddress addr in Device.Addresses)
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
            get { return Communicator != null; }
            set
            {
                if (value)
                {
                    if (Listening)
                    {
                        StopListening();
                    }
                    StartListening();
                }
                else
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
            packetProcessorWorker?.CancelAsync();
        }

        /// <summary>
        /// Launch a background thread to listen for packets on this interface
        /// </summary>
        private void StartListening()
        {
            ErrorMessage = "";

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
                    ErrorMessage = "Not an Ethernet interface.";
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
                ErrorMessage = "Failed to open interface: " + ex.GetType() + ".";
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
            ErrorMessage = "";
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
                            ObservedHost host = new ObservedHost(packet.Ethernet.Source, packet.Ethernet.IpV4.Source, IpV4Address);
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
                ErrorMessage = "Failed to listen: " + ex.GetType() + ".";
            }
            finally
            {
                Communicator = null;
            }
        }
    }

}
