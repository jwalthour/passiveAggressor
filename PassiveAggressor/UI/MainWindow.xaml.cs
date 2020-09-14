using PassiveAggressor.UI;
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
        private SortedDictionary<string, HostGroup> hostGroupControls = new SortedDictionary<string, HostGroup>();
        private HostGroup expandedHostGroup = null;

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
            // If we still have that label, remove it
            if (stackHostList.Children.Count > 0 && stackHostList.Children[0] is Label)
            {
                stackHostList.Children.Clear();
            }
            foreach (KeyValuePair<string, Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>> hostGroup in hosts)
            {
                HostGroup hostGroupControl = null;
                if(hostGroupControls.ContainsKey(hostGroup.Key))
                {
                    hostGroupControl = hostGroupControls[hostGroup.Key];
                } 
                else
                {
                    hostGroupControl = new HostGroup(hostGroup.Key, nm.GetIconResourceNameForMfr(hostGroup.Key));
                    hostGroupControl.UserExpandedHost = UserExpandedHostGroup;
                    hostGroupControls.Add(hostGroup.Key, hostGroupControl);
                    // TODO: sort alphabetically.  Might be easier if we make hostGroupControls a sorted list or something so we can do stackHostList.Children.Insert
                    stackHostList.Children.Add(hostGroupControl);
                }

                hostGroupControl.UpdateVisibleHostsList(hostGroup.Value);

            }

            //hostControls.Sort(CompareHostsForList);
            //foreach (UI.VisibleHost hostControl in hostControls)
            //{
            //    stackHostList.Children.Add(hostControl);
            //}
        }

        /// <summary>
        /// Called when the user clicks the "expand" button on the host list for a particular manufacturer
        /// </summary>
        /// <param name="justExpanded"></param>
        private void UserExpandedHostGroup(HostGroup justExpanded)
        {
            if(expandedHostGroup != null && expandedHostGroup != justExpanded)
            {
                expandedHostGroup.CollapseHostList();
            }
            expandedHostGroup = justExpanded;
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


        private void ButtonClearHosts_Click(object sender, RoutedEventArgs e)
        {
            nm.ClearHostsList();
            stackHostList.Children.Clear();
        }
    }
}