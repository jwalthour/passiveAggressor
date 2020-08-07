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
            labelMacAddress.Content = mac.ToString();
            string mfrName = ManufacturerData.instance.GetMfrNameForMac(mac);
            labelMfrString.Content = mfrName;
            string mfrIconResource = ManufacturerData.instance.GetIconResourceNameForMfr(mfrName);
            imageMfrIcon.Source = LoadImage(mfrIconResource);
        }

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
    }
}
