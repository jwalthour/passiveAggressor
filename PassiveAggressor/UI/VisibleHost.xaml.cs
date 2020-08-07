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

namespace PassiveAggressor.UI
{
    /// <summary>
    /// Interaction logic for VisibleHost.xaml
    /// </summary>
    public partial class VisibleHost : UserControl
    {
        public VisibleHost()
        {
            InitializeComponent();
        }
        public VisibleHost(PcapDotNet.Packets.Ethernet.MacAddress mac, PcapDotNet.Packets.IpV4.IpV4Address ip)
        {
            InitializeComponent();
            labelIpV4Address.Content = ip.ToString();
            labelMacAddress.Content = mac.ToString();
            labelMfrString.Content = ManufacturerData.instance.GetMfrNameForMac(mac);
        }
    }
}
