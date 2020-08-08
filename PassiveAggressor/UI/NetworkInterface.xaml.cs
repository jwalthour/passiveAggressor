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
            labelIpv4Address.Content = intf.IpV4Address != null ? (intf.IpV4Address.Address as PcapDotNet.Core.IpV4SocketAddress).Address.ToString() : "";
            UpdateListenButtonEnables(intf.Listening);

            intf.ListeningChanged += Intf_ListeningChanged;
        }

        private void Intf_ListeningChanged(bool isListeningNow)
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateListenButtonEnables(isListeningNow)));
        }

        /// <summary>
        /// Update the enable/disable states of the Listen and Ignore buttons based 
        /// on whether the interface is presently listening
        /// </summary>
        /// <param name="isListening"></param>
        private void UpdateListenButtonEnables(bool isListening)
        {
            buttonListen.IsEnabled = !isListening;
            buttonIgnore.IsEnabled = isListening;
        }

        private void ButtonListen_Click(object sender, RoutedEventArgs e)
        {
            // Both buttons will be disabled until the listen/ignore completes
            buttonListen.IsEnabled = false;
            intf.StartListening();
        }

        private void ButtonIgnore_Click(object sender, RoutedEventArgs e)
        {
            // Both buttons will be disabled until the listen/ignore completes
            buttonIgnore.IsEnabled = false;
            intf.StopListening();
        }

        private void ButtonPingSubnet_Click(object sender, RoutedEventArgs e)
        {
            buttonPingSubnet.IsEnabled = false;
            buttonCancelSubnetPing.IsEnabled = true;
            intf.BeginPingingSubnet(PingSweepCompleted, PingSweepProgress);
        }

        private void ButtonCancelSubnetPing_Click(object sender, RoutedEventArgs e)
        {
            intf.CancelPingingSubnet();
        }

        /// <summary>
        /// Callled when the subnet ping stops
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PingSweepCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            buttonPingSubnet.IsEnabled = true;
            buttonCancelSubnetPing.IsEnabled = false;
        }

        /// <summary>
        /// Called when the subnet ping reports progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PingSweepProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressPingSweep.Value = e.ProgressPercentage;
        }
    }
}
