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
    /// Interaction logic for NetworkInterface.xaml
    /// </summary>
    public partial class NetworkInterface : UserControl
    {
        private ListeningInterface intf;

        public NetworkInterface()
        {
            InitializeComponent();
        }

        public NetworkInterface(ListeningInterface intf)
        {
            this.intf = intf;
            InitializeComponent();

            labelDescription.Content = intf.Description;
            labelIpv4Address.Content = intf.IpV4Address != null? (intf.IpV4Address.Address as PcapDotNet.Core.IpV4SocketAddress).Address.ToString() : "";
            checkboxListen.IsChecked = intf.Listening;
        }

        private void CheckboxListen_Checked(object sender, RoutedEventArgs e)
        {
            intf.Listening = checkboxListen.IsChecked.Value;
        }
    }
}
