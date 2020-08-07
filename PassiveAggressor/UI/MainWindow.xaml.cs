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

        private void Nm_HostListChanged(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, NetworkMonitor.Host> hosts)
        {
            // Run it on the GUI thread
            Dispatcher.BeginInvoke(new Action(() => UpdateVisibleHostsList(hosts)));
        }

        private void UpdateVisibleHostsList(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, NetworkMonitor.Host> hosts)
        {
            stackHostList.Children.Clear();

            foreach (KeyValuePair<PcapDotNet.Packets.Ethernet.MacAddress, NetworkMonitor.Host> host in hosts)
            {
                UI.VisibleHost hostControl = new UI.VisibleHost(host.Value.HostMacAddress, host.Value.HostIpV4Address);
                stackHostList.Children.Add(hostControl);
            }
            //Console.WriteLine("");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            nm.InitializeInterfaces();
            // TODO: launch in background
            UI.ManufacturerData.instance.LoadMfrData();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Button_OnePacket_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}