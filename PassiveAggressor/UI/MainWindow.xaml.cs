using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace PassiveAggressor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkMonitor nm = new NetworkMonitor();
        public MainWindow()
        {
            InitializeComponent();

            Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string semanticVersion = assemblyVersion.Major + "." + assemblyVersion.Minor + "." + assemblyVersion.Revision;
            Title += " version " + semanticVersion;

            Loaded += MainWindow_Loaded;
            nm.HostListChanged += Nm_HostListChanged;
        }

        private void Nm_HostListChanged(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost> hosts)
        {
            // Run it on the GUI thread
            Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost> hostsShallowCopy = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost>(hosts);
            Dispatcher.BeginInvoke(new Action(() => UpdateVisibleHostsList(hostsShallowCopy)));
        }

        private void UpdateVisibleHostsList(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost> hosts)
        {
            // TODO: rather than replacing the list every time, check to see if the control for each host already exists, then insert new hosts in a sorted fashion
            stackHostList.Children.Clear();
            List<UI.VisibleHost> hostControls = new List<UI.VisibleHost>();
            foreach (KeyValuePair<PcapDotNet.Packets.Ethernet.MacAddress, ObservedHost> host in hosts)
            {
                UI.VisibleHost hostControl = new UI.VisibleHost(host.Value.HostMacAddress, host.Value.HostIpV4Address);
                hostControls.Add(hostControl);
            }

            hostControls.Sort(CompareHostsForList);
            foreach(UI.VisibleHost hostControl in hostControls)
            {
                stackHostList.Children.Add(hostControl);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            nm.InitializeInterfaces();
            UI.ManufacturerData.instance.LoadMfrData();
            UI.NicknameData.instance.LoadNicknameData();
            PopulateInterfaceList();
        }

        /// <summary>
        /// Get the list of interfaces and display them
        /// </summary>
        private void PopulateInterfaceList()
        {
            stackInterfaceList.Children.Clear();
            List<ListeningInterface> interfaces = nm.Interfaces.Values.ToList();
            // Put the ones with IP addresses first in the list
            interfaces.Sort(CompareInterfacesForList);
            foreach (ListeningInterface intf in interfaces)
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
        private int CompareInterfacesForList(ListeningInterface a, ListeningInterface b)
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
            if(a.HasNickname && !b.HasNickname)
            {
                return -1;
            }
            else if (!a.HasNickname && b.HasNickname)
            {
                return 1;
            }
            else
            {
                return a.Mac.ToString().CompareTo(b.Mac.ToString());
            }
        }

        private void ButtonClearHosts_Click(object sender, RoutedEventArgs e)
        {
            nm.ClearHostsList();
        }
    }
}