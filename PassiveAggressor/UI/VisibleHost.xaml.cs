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

        public VisibleHost(PcapDotNet.Packets.Ethernet.MacAddress mac, PcapDotNet.Packets.IpV4.IpV4Address ip)
        {
            InitializeComponent();
            labelIpV4Address.Content = ip.ToString();
            Mac = mac;
        }

        /// <summary>
        /// Backing variable for the Mac property - use the property instead
        /// </summary>
        private PcapDotNet.Packets.Ethernet.MacAddress _mac;
        /// <summary>
        /// Set will also update the GUI fields that are based on the MAC address
        /// </summary>
        public PcapDotNet.Packets.Ethernet.MacAddress Mac
        {
            get { return _mac; }
            set
            {
                _mac = value;
                labelMacAddress.Content = _mac.ToString();
                string mfrName = ManufacturerData.instance.GetMfrNameForMac(_mac);
                labelMfrString.Content = mfrName;
                string mfrIconResource = ManufacturerData.instance.GetIconResourceNameForMfr(mfrName);
                imageMfrIcon.Source = LoadImage(mfrIconResource);
                string nickname = NicknameData.instance.GetNicknameForMac(_mac);
                labelNickname.Text = nickname;
            }
        }

        /// <summary>
        /// True if this host has a user-entered nickname
        /// </summary>
        public bool HasNickname { get { return labelNickname.Text.Length > 0; } }

        /// <summary>
        /// Load an image from the indicated embedded resource
        /// </summary>
        /// <param name="resourceName">Path to an embedded PNG resource</param>
        /// <returns></returns>
        private ImageSource LoadImage(string resourceName)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            BitmapImage bitmap = new BitmapImage();

            using (System.IO.Stream stream =
                assembly.GetManifestResourceStream(resourceName))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }

            return bitmap;
        }

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

        }

        private void TextBoxEnterNickname_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SaveEnteredNickname();
            }
        }
    }
}
