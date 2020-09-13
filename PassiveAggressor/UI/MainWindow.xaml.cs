using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PassiveAggressor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Network.NetworkMonitor nm = new Network.NetworkMonitor();

        public MainWindow()
        {
            InitializeComponent();

            Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string semanticVersion = assemblyVersion.Major + "." + assemblyVersion.Minor + "." + assemblyVersion.Revision;
            Title += " version " + semanticVersion;

            Loaded += MainWindow_Loaded;
            nm.HostListChanged += Nm_HostListChanged;
        }

        private void Nm_HostListChanged(Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>> hosts)
        {
            // Run it on the GUI thread
            Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>> hostsShallowCopy = new Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>>(hosts);
            Dispatcher.BeginInvoke(new Action(() => UpdateVisibleHostsList(hostsShallowCopy)));
        }

        /// <summary>
        /// Called whenever the NetworkMonitor reports a chang ein the hosts list.  Updates the host list in the GUI.
        /// </summary>
        /// <param name="hosts"></param>
        private void UpdateVisibleHostsList(Dictionary<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>> hosts)
        {
            foreach (KeyValuePair<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>> hostGroup in hosts)
            {
                foreach (KeyValuePair<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> host in hostGroup.Value)
                {

                    // If we still have that label, remove it
                    if (stackHostList.Children.Count > 0 && stackHostList.Children[0] is Label)
                    {
                        stackHostList.Children.Clear();
                    }

                    // Find where this host would go in the list.  If it's not there, insert it.
                    int i = 0;
                    bool shouldAdd = stackHostList.Children.Count == 0;
                    UI.VisibleHost newHostControl = new UI.VisibleHost(host.Value, nm.GetIconResourceNameForMfr(host.Value.ManufacturerDescription));
                    newHostControl.NicknameUpdated += UserSetAHostNickname;
                    foreach (object control in stackHostList.Children)
                    {
                        UI.VisibleHost existingHostControl = control as UI.VisibleHost;
                        int sortOrder = CompareHostsForList(existingHostControl, newHostControl);
                        if (sortOrder == 0)
                        {
                            // This host already exists in the list
                            // TODO: if we start displaying "last seen" value, update that
                            shouldAdd = false;
                            break;
                        }
                        else if (sortOrder < 0)
                        {
                            // This host goes somewhere after the existing control.
                            // Keep going
                            shouldAdd = true;
                        }
                        else if (sortOrder > 0)
                        {
                            // We found the first existing host that goes after this host, indicating we've found the spot in the list to insert this host
                            shouldAdd = true;
                            break;
                        }
                        i++;
                    }

                    if (shouldAdd)
                    {
                        if (i < stackHostList.Children.Count)
                        {
                            stackHostList.Children.Insert(i, newHostControl);
                        }
                        else
                        {
                            stackHostList.Children.Add(newHostControl);
                        }
                    }
                }
            }

            //hostControls.Sort(CompareHostsForList);
            //foreach (UI.VisibleHost hostControl in hostControls)
            //{
            //    stackHostList.Children.Add(hostControl);
            //}
        }

        /// <summary>
        /// Called whenever an event informs us the user has updated (edited, added, or deleted) a host nickname
        /// </summary>
        private void UserSetAHostNickname(UI.VisibleHost hostControlUpdated)
        {
            // Re-sort the hosts list
            List<UI.VisibleHost> sortedHosts = new List<UI.VisibleHost>();
            foreach(UI.VisibleHost existingHost in stackHostList.Children)
            {
                sortedHosts.Add(existingHost);
            }
            sortedHosts.Sort(CompareHostsForList);
            stackHostList.Children.Clear();
            foreach(UI.VisibleHost existingHost in sortedHosts)
            {
                stackHostList.Children.Add(existingHost);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UI.NicknameData.instance.LoadNicknameData();
            nm.Initialize();
            PopulateInterfaceList();
        }

        /// <summary>
        /// Get the list of interfaces and display them
        /// </summary>
        private void PopulateInterfaceList()
        {
            stackInterfaceList.Children.Clear();
            List<Network.ListeningInterface> interfaces = nm.Interfaces.Values.ToList();
            // Put the ones with IP addresses first in the list
            interfaces.Sort(CompareInterfacesForList);
            foreach (Network.ListeningInterface intf in interfaces)
            {
                // Only show the interfaces that started up properly
                if (intf.ErrorMessage.Length == 0)
                {
                    UI.NetworkInterface intfControl = new UI.NetworkInterface(intf);
                    stackInterfaceList.Children.Add(intfControl);
                }
            }
        }

        /// <summary>
        /// Used for sorting network interfaces in interface list.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 to indicate a should go first, 0 to indicate sameness, 1 to indicate b should go first</returns>
        private int CompareInterfacesForList(Network.ListeningInterface a, Network.ListeningInterface b)
        {
            if (a.IpV4Address != null && b.IpV4Address == null)
            {
                return -1;
            }
            else if (a.IpV4Address == null && b.IpV4Address != null)
            {
                return 1;
            }

            // Last sort criteria: description string
            return a.Description.CompareTo(b.Description);
        }

        /// <summary>
        /// Used for sorting hosts in hosts list
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 to indicate a should go first, 0 to indicate sameness, 1 to indicate b should go first</returns>
        private int CompareHostsForList(UI.VisibleHost a, UI.VisibleHost b)
        {
            if (a.HasNickname && !b.HasNickname)
            {
                return -1;
            }
            else if (!a.HasNickname && b.HasNickname)
            {
                return 1;
            }
            else
            {
                return a.Mac.CompareTo(b.Mac);
            }
        }

        private void ButtonClearHosts_Click(object sender, RoutedEventArgs e)
        {
            nm.ClearHostsList();
            stackHostList.Children.Clear();
        }
    }
}