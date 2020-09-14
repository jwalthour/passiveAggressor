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
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class HostGroup : UserControl
    {
        private string mfrDesc;
        private Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> hosts;
        
        /// <summary>
        /// WIll be called when the user expands this group
        /// </summary>
        /// <param name="justClicked"></param>
        public delegate void UserExpandedHostGroup_d(HostGroup justClicked);
        public UserExpandedHostGroup_d UserExpandedHost = null;

        public HostGroup(string mfrDesc, string iconResourceName)
        {
            InitializeComponent();

            this.mfrDesc = mfrDesc;
            imageMfrIcon.Source = LoadImage(iconResourceName);
        }

        public void UpdateVisibleHostsList(Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> Hosts)
        {
            hosts = new Dictionary<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost>(Hosts);
            labelMfrString.Content = mfrDesc + "\n(" + Hosts.Count.ToString() + " hosts)";

            if (expanderHostsList.IsExpanded)
            {
                PopulateHostList();
            }
        }

        public void CollapseHostList()
        {
            expanderHostsList.IsExpanded = false;
        }

        /// <summary>
        /// Called whenever an event informs us the user has updated (edited, added, or deleted) a host nickname
        /// </summary>
        private void SortHostList(UI.VisibleHost hostControlUpdated)
        {
            // Re-sort the hosts list
            List<UI.VisibleHost> sortedHosts = new List<UI.VisibleHost>();
            foreach (UI.VisibleHost existingHost in stackHostList.Children)
            {
                sortedHosts.Add(existingHost);
            }
            sortedHosts.Sort(CompareHostsForList);
            stackHostList.Children.Clear();
            foreach (UI.VisibleHost existingHost in sortedHosts)
            {
                stackHostList.Children.Add(existingHost);
            }
        }


        /// <summary>
        /// Used for sorting hosts in hosts list
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 to indicate a should go first, 0 to indicate sameness, 1 to indicate b should go first</returns>
        private int CompareHostsForList(UI.VisibleHost a, UI.VisibleHost b)
        {
            if (a.HasNickname && !b.HasNickname)
            {
                return -1;
            }
            else if (!a.HasNickname && b.HasNickname)
            {
                return 1;
            }
            else
            {
                return a.Mac.CompareTo(b.Mac);
            }
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

        private void expanderHostsList_Collapsed(object sender, RoutedEventArgs e)
        {

        }

        private void expanderHostsList_Expanded(object sender, RoutedEventArgs e)
        {
            UserExpandedHost?.Invoke(this);
            PopulateHostList();
        }

        private void PopulateHostList()
        {
            foreach (KeyValuePair<PcapDotNet.Packets.Ethernet.MacAddress, Network.ObservedHost> host in hosts)
            {
                // Find where this host would go in the list.  If it's not there, insert it.
                int i = 0;
                bool shouldAdd = stackHostList.Children.Count == 0;
                UI.VisibleHost newHostControl = new UI.VisibleHost(host.Value);
                newHostControl.NicknameUpdated += SortHostList;
                foreach (object control in stackHostList.Children)
                {
                    UI.VisibleHost existingHostControl = control as UI.VisibleHost;
                    int sortOrder = CompareHostsForList(existingHostControl, newHostControl);
                    if (sortOrder == 0)
                    {
                        // This host already exists in the list
                        // TODO: if we start displaying "last seen" value, update that
                        shouldAdd = false;
                        break;
                    }
                    else if (sortOrder < 0)
                    {
                        // This host goes somewhere after the existing control.
                        // Keep going
                        shouldAdd = true;
                    }
                    else if (sortOrder > 0)
                    {
                        // We found the first existing host that goes after this host, indicating we've found the spot in the list to insert this host
                        shouldAdd = true;
                        break;
                    }
                    i++;
                }

                if (shouldAdd)
                {
                    if (i < stackHostList.Children.Count)
                    {
                        stackHostList.Children.Insert(i, newHostControl);
                    }
                    else
                    {
                        stackHostList.Children.Add(newHostControl);
                    }
                }
            }
        }
    }
}
