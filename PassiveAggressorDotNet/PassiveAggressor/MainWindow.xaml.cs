using System;
using System.Collections.Generic;
using System.Linq;
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
            Loaded += MainWindow_Loaded;
            nm.HostListChanged += Nm_HostListChanged;
        }

        private void Nm_HostListChanged(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, NetworkMonitor.Host> hosts)
        {

            foreach(KeyValuePair<PcapDotNet.Packets.Ethernet.MacAddress, NetworkMonitor.Host> host in hosts)
            {
                Console.WriteLine("Host: " + host.Key + " " + host.Value.HostIpV4Address);
            }
            Console.WriteLine("");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            nm.InitializeInterfaces();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Button_OnePacket_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}