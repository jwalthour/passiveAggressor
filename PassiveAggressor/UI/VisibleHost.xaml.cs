using PcapDotNet.Packets.Ethernet;
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
        //TODO: this would be better if it was configurable
        private const string FILEZILLA_PATH = "C:\\Program Files\\FileZilla FTP Client\\filezilla.exe";
        public VisibleHost()
        {
            InitializeComponent();
        }

        public VisibleHost(Network.ObservedHost host)
        {
            InitializeComponent();
            labelIpV4Address.Content = host.HostIpV4Address.ToString();
            Mac = host.HostMacAddress;
            labelMacAddress.Content = Mac.ToString();
            string nickname = NicknameData.instance.GetNicknameForMac(Mac);
            labelNickname.Text = nickname;
        }

        /// <summary>
        /// Set will also update the GUI fields that are based on the MAC address
        /// </summary>
        public MacAddress Mac { get; private set; }

        public delegate void NicknameUpdated_d(VisibleHost sender);
        /// <summary>
        /// This event will be fired whenever the user updates the label on this host
        /// </summary>
        public event NicknameUpdated_d NicknameUpdated;

        /// <summary>
        /// True if this host has a user-entered nickname
        /// </summary>
        public bool HasNickname { get { return labelNickname.Text.Length > 0; } }

        private void ButtonHttp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://" + labelIpV4Address.Content + "/");
        }

        private void ButtonHttps_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://" + labelIpV4Address.Content + "/");
        }

        private void ButtonSsh_Click(object sender, RoutedEventArgs e)
        {
            string myPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string puttyPath = myPath + "\\tools\\putty\\putty.exe";
            try
            {
                System.Diagnostics.Process.Start(puttyPath, (string)labelIpV4Address.Content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't start putty: " + ex);
            }
        }

        private void ButtonSftp_Click(object sender, RoutedEventArgs e)
        {
            LaunchFileZilla((string)labelIpV4Address.Content, "sftp");
        }

        private void ButtonFtp_Click(object sender, RoutedEventArgs e)
        {
            LaunchFileZilla((string)labelIpV4Address.Content, "sftp");
        }

        /// <summary>
        /// Launch FileZilla to connect to the given host
        /// </summary>
        /// <param name="host">Host IP address or DNS name</param>
        /// <param name="protocol">string protocol prefix, eg "sftp" or "ftp".  Don't knclude "://"</param>
        private void LaunchFileZilla(string host, string protocol)
        {
            try
            {
                System.Diagnostics.Process.Start(FILEZILLA_PATH, protocol + "://" + host + " --logontype=ask");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't start FileZilla: " + ex);
            }
        }

        private void ButtonEditNickname_Click(object sender, RoutedEventArgs e)
        {
            textBoxEnterNickname.Text = labelNickname.Text;

            labelNickname.Visibility = Visibility.Hidden;
            buttonEditNickname.Visibility = Visibility.Hidden;

            textBoxEnterNickname.Visibility = Visibility.Visible;
            buttonSaveNickname.Visibility = Visibility.Visible;
            textBoxEnterNickname.Focus();
            textBoxEnterNickname.SelectAll();
        }

        private void ButtonSaveNickname_Click(object sender, RoutedEventArgs e)
        {
            SaveEnteredNickname();
        }

        /// <summary>
        /// Save whatever nickname has been entered in the entry field
        /// </summary>
        private void SaveEnteredNickname()
        {
            NicknameData.instance.SetNicknameForMac(Mac, textBoxEnterNickname.Text);
            labelNickname.Text = NicknameData.instance.GetNicknameForMac(Mac);

            labelNickname.Visibility = Visibility.Visible;
            buttonEditNickname.Visibility = Visibility.Visible;

            textBoxEnterNickname.Visibility = Visibility.Hidden;
            buttonSaveNickname.Visibility = Visibility.Hidden;

            NicknameUpdated?.Invoke(this);
        }

        private void TextBoxEnterNickname_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SaveEnteredNickname();
            }
        }

        private void TextBoxEnterNickname_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveEnteredNickname();
        }

        private void buttonCopyIpAddress_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(DataFormats.Text, labelIpV4Address.Content);
        }

        private void buttonCopyMacAddress_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(DataFormats.Text, labelMacAddress.Content);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
