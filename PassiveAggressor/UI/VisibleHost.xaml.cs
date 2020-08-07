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

        private void ButtonPutty_Click(object sender, RoutedEventArgs e)
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

        private void ButtonFileZilla_Click(object sender, RoutedEventArgs e)
        {
            //TODO: this would be better if it was configurable (eg also just plain old FTP rather than always SFTP)
            string fzPath = "C:\\Program Files\\FileZilla FTP Client\\filezilla.exe";
            try
            {
                System.Diagnostics.Process.Start(fzPath, "sftp://" + (string)labelIpV4Address.Content + " --logontype=ask");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't start FileZilla: " + ex);
            }
        }
    }
}
